using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	public class ElympicsBehavioursManager : MonoBehaviour
	{
		[SerializeField] private ElympicsBehavioursSerializableDictionary elympicsBehavioursView = new ElympicsBehavioursSerializableDictionary();

		private          ElympicsBehavioursContainer _elympicsBehaviours;
		private readonly List<ElympicsBehaviour>     _bufferForIteration = new List<ElympicsBehaviour>();
		private          IElympics                   _elympics;
		private          BinaryInputWriter           _inputWriter;
		private          BinaryInputReader           _inputReader;

		internal void InitializeInternal(IElympics elympics)
		{
			_inputWriter = new BinaryInputWriter();
			_inputReader = new BinaryInputReader();

			_elympics = elympics;
			_elympicsBehaviours = new ElympicsBehavioursContainer(_elympics.PlayerId);
			var foundElympicsBehaviours = gameObject.FindObjectsOfTypeOnScene<ElympicsBehaviour>(true);
			foreach (var elympicsBehaviour in foundElympicsBehaviours)
			{
				var networkId = elympicsBehaviour.NetworkId;
				if (networkId < 0)
				{
					Debug.LogError($"Invalid networkId {networkId} on {elympicsBehaviour.gameObject.name} {elympicsBehaviour.GetType().Name}", elympicsBehaviour);
					return;
				}

				if (_elympicsBehaviours.Contains(networkId))
				{
					Debug.LogError($"Duplicated networkId {networkId} on {elympicsBehaviour.gameObject.name} {elympicsBehaviour.GetType().Name}", elympicsBehaviour);
					return;
				}

				InitializeElympicsBehaviour(elympicsBehaviour);
				_elympicsBehaviours.Add(elympicsBehaviour);
			}
		}

		private void OnDestroy()
		{
			_inputWriter?.Dispose();
			_inputReader?.Dispose();
		}

		private void InitializeElympicsBehaviour(ElympicsBehaviour elympicsBehaviour)
		{
			elympicsBehaviour.InitializeInternal(_elympics);
		}

		internal void AddNewBehaviour(ElympicsBehaviour elympicsBehaviour)
		{
			InitializeElympicsBehaviour(elympicsBehaviour);
			_elympicsBehaviours.Add(elympicsBehaviour);
		}

		internal void RemoveBehaviour(int networkId)
		{
			_elympicsBehaviours.Remove(networkId);
		}

		internal ElympicsInput GetInputForClient() => GetInput(ClientInputGetter);
		internal ElympicsInput GetInputForBot()    => GetInput(BotInputGetter);

		private static void ClientInputGetter(ElympicsBehaviour behaviour, BinaryInputWriter writer) => behaviour.GetInputForClient(writer);
		private static void BotInputGetter(ElympicsBehaviour behaviour, BinaryInputWriter writer)    => behaviour.GetInputForBot(writer);

		private ElympicsInput GetInput(Action<ElympicsBehaviour, BinaryInputWriter> getInput)
		{
			var input = new ElympicsInput
			{
				Data = new List<KeyValuePair<int, byte[]>>()
			};

			foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.BehavioursWithInput)
			{
				if (!elympicsBehaviour.HasAnyInput)
					continue;

				getInput(elympicsBehaviour, _inputWriter);

				var serializedData = _inputWriter.GetData();
				if (serializedData != null && serializedData.Length != 0)
					input.Data.Add(new KeyValuePair<int, byte[]>(networkId, serializedData));
				_inputWriter.ResetStream();
			}

			return input;
		}

		internal void ApplyInput(ElympicsInput input)
		{
			foreach (var data in input.Data)
			{
				if (_elympicsBehaviours.BehavioursWithInput.TryGetValue(data.Key, out var elympicsBehaviour))
				{
					_inputReader.FeedDataForReading(data.Value);
					try
					{
						elympicsBehaviour.ApplyInput(input.PlayerId, _inputReader);
					}
					catch (EndOfStreamException)
					{
						throw new ReadTooMuchException(elympicsBehaviour);
					}

					if (!_inputReader.AllBytesRead())
						throw new ReadNotEnoughException(elympicsBehaviour);
				}
			}
		}

		internal ElympicsSnapshot GetSnapshot()
		{
			var snapshot = new ElympicsSnapshot {Data = new List<KeyValuePair<int, byte[]>>()};

			foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
			{
				if (!elympicsBehaviour.HasAnyState)
					continue;

				snapshot.Data.Add(new KeyValuePair<int, byte[]>(networkId, elympicsBehaviour.GetState()));
			}

			return snapshot;
		}

		internal Dictionary<int, ElympicsSnapshot> GetSnapshots(params int[] players)
		{
			var snapshots = new Dictionary<int, ElympicsSnapshot>();
			foreach (var player in players)
				snapshots[player] = new ElympicsSnapshot {Data = new List<KeyValuePair<int, byte[]>>()};

			foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
			{
				if (!elympicsBehaviour.HasAnyState)
					continue;

				var stateData = new KeyValuePair<int, byte[]>(networkId, elympicsBehaviour.GetState());

				foreach (var player in players)
				{
					if (elympicsBehaviour.IsVisibleTo(player))
						snapshots[player].Data.Add(stateData);
				}
			}

			return snapshots;
		}

		internal void ApplySnapshot(ElympicsSnapshot elympicsSnapshot, StatePredictability statePredictability = StatePredictability.Both)
		{
			var chosenElympicsBehaviours = GetElympicsBehavioursBasedOnStatePredictability(statePredictability);

			foreach (var data in elympicsSnapshot.Data)
			{
				if (chosenElympicsBehaviours.TryGetValue(data.Key, out var elympicsBehaviour))
					elympicsBehaviour.ApplyState(data.Value);
			}
		}

		private IReadOnlyDictionary<int, ElympicsBehaviour> GetElympicsBehavioursBasedOnStatePredictability(StatePredictability statePredictability)
		{
			switch (statePredictability)
			{
				case StatePredictability.Predictable:
					return _elympicsBehaviours.BehavioursPredictable;
				case StatePredictability.Unpredictable:
					return _elympicsBehaviours.BehavioursUnpredictable;
				case StatePredictability.Both:
					return _elympicsBehaviours.Behaviours;
				default:
					throw new ArgumentOutOfRangeException(nameof(statePredictability), statePredictability, null);
			}
		}

		internal bool AreSnapshotsEqualOnPredictableBehaviours(ElympicsSnapshot historySnapshot, ElympicsSnapshot receivedSnapshot)
		{
			if (historySnapshot.Data.Count != receivedSnapshot.Data.Count)
			{
				Debug.Log("History snapshot size not equal received snapshot size");
				return false;
			}

			var chosenElympicsBehaviours = _elympicsBehaviours.BehavioursPredictable;
			for (var i = 0; i < historySnapshot.Data.Count; i++)
			{
				var historyState = historySnapshot.Data[i];
				var receivedState = receivedSnapshot.Data[i];

				if (historyState.Key != receivedState.Key)
				{
					Debug.Log($"History snapshot key {historyState.Key} not equal received snapshot key {receivedState.Key} on same snapshot position");
					return false;
				}

				var networkId = historyState.Key;
				if (!chosenElympicsBehaviours.TryGetValue(networkId, out var elympicsBehaviour))
					continue;

				if (!elympicsBehaviour.AreStatesEqual(historyState.Value, receivedState.Value))
				{
					Debug.LogWarning($"States not equal on {networkId}");
					return false;
				}
			}

			return true;
		}

		internal void PredictableFixedUpdate()
		{
			// copy behaviours to list before iterating because the collection might be modified by Instantiate/Destroy
			_bufferForIteration.Clear();
			_bufferForIteration.AddRange(_elympicsBehaviours.Behaviours.Values);
			foreach (var elympicsBehaviour in _bufferForIteration)
				elympicsBehaviour.PredictableFixedUpdate();
		}

		internal void OnPreReconcile()
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnPreReconcile();
		}

		internal void OnPostReconcile()
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnPostReconcile();
		}

		internal void RefreshElympicsBehavioursView()
		{
			elympicsBehavioursView.Clear();
			var foundElympicsBehaviours = gameObject.FindObjectsOfTypeOnScene<ElympicsBehaviour>(true);
			foreach (var elympicsBehaviour in foundElympicsBehaviours)
			{
				var networkId = elympicsBehaviour.NetworkId;
				if (elympicsBehavioursView.ContainsKey(networkId))
				{
					Debug.LogWarning($"Cannot refresh behaviour with networkId {networkId}! Duplicated entry", elympicsBehaviour);
					continue;
				}

				elympicsBehavioursView.Add(networkId, elympicsBehaviour);
			}
		}

		#region ClientCallbacks

		internal void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnStandaloneClientInit(data);
		}

		internal void OnClientsOnServerInit(InitialMatchPlayerDatas data)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnClientsOnServerInit(data);
		}

		internal void OnSynchronized(TimeSynchronizationData data)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnSynchronized(data);
		}

		internal void OnMatchJoinedFailed(string errorMessage)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnMatchJoinedFailed(errorMessage);
		}

		internal void OnMatchJoined(string matchId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnMatchJoined(matchId);
		}

		internal void OnMatchEnded(string matchId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnMatchEnded(matchId);
		}

		internal void OnAuthenticatedFailed(string errorMessage)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnAuthenticatedFailed(errorMessage);
		}

		internal void OnAuthenticated(string userId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnAuthenticated(userId);
		}

		internal void OnDisconnectedByServer()
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnDisconnectedByServer();
		}

		internal void OnDisconnectedByClient()
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnDisconnectedByClient();
		}

		internal void OnConnected(TimeSynchronizationData data)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnConnected(data);
		}

		internal void OnConnectingFailed()
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnConnectingFailed();
		}

		#endregion

		#region BotCallbacks

		internal void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnStandaloneBotInit(initialMatchData);
		}

		internal void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchData)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnBotsOnServerInit(initialMatchData);
		}

		#endregion

		#region ServerCallbacks

		internal void OnServerInit(InitialMatchPlayerDatas initialMatchData)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnServerInit(initialMatchData);
		}

		internal void OnPlayerConnected(int playerId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnPlayerConnected(playerId);
		}

		internal void OnPlayerDisconnected(int playerId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnPlayerDisconnected(playerId);
		}

		#endregion

		internal enum StatePredictability
		{
			Predictable,
			Unpredictable,
			Both
		}
	}
}
