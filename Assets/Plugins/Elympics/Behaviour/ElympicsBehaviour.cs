using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MatchTcpClients.Synchronizer;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Elympics
{
	/// <summary>
	/// Used for object state synchronization. Allows scripts attached to the object to implement interfaces inheriting from <see cref="IObservable"/>.
	/// </summary>
	[ExecuteInEditMode]
	public sealed class ElympicsBehaviour : MonoBehaviour
	{
		internal const int UndefinedNetworkId = -1;

		[SerializeField] internal bool forceNetworkId     = false;
		[SerializeField] internal int  networkId          = UndefinedNetworkId;
		[SerializeField] internal int  predictabilityMask = (int) ElympicsBehaviourPlayersMask.All;
		[SerializeField] internal int  visibilityMask     = (int) ElympicsBehaviourPlayersMask.All;

		private ElympicsComponentsContainer _componentsContainer;
		private List<ElympicsVar>           _backingFields;

		internal bool HasAnyState => _componentsContainer.Observables.Length > 0;
		internal bool HasAnyInput => _componentsContainer.InputHandlers.Length > 0;

		public int NetworkId
		{
			get => networkId;
			internal set => networkId = value;
		}

		internal IElympics Controller                    { get; private set; }
		public   bool      IsPredictableTo(int playerId) => playerId == ElympicsPlayer.WORLD_ID || (predictabilityMask & playerId.GetPlayerIdMask()) > 0;
		internal bool      IsVisibleTo(int playerId)     => playerId == ElympicsPlayer.WORLD_ID || (visibilityMask & playerId.GetPlayerIdMask()) > 0;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!forceNetworkId && networkId == UndefinedNetworkId)
				UpdateSerializedNetworkId();
		}

		private Texture2D _gizmo = null;

		private void OnEnable()
		{
			if (!forceNetworkId && IsMyNetworkIdTaken())
				UpdateSerializedNetworkId();
		}

		private bool IsMyNetworkIdTaken()
		{
			return FindObjectsOfType<ElympicsBehaviour>()
				.Where(behaviour => behaviour != this)
				.Select(behaviour => behaviour.NetworkId)
				.Contains(networkId);
		}

		internal void UpdateSerializedNetworkId()
		{
			networkId = NetworkIdEnumerator.Instance.MoveNextAndGetCurrent();
			EditorUtility.SetDirty(this);
		}

		private void OnDrawGizmos()
		{
		}
#endif

		internal void InitializeInternal(IElympics elympics)
		{
			Controller = elympics;

			_componentsContainer = new ElympicsComponentsContainer(this);

			foreach (var initializable in _componentsContainer.Initializables)
				initializable.Initialize();

			var elympicsVarType = typeof(ElympicsVar);
			_backingFields = new List<ElympicsVar>();
			foreach (var observable in _componentsContainer.Observables)
			{
				foreach (var field in observable.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				{
					if (elympicsVarType.IsAssignableFrom(field.FieldType))
					{
						var value = field.GetValue(observable) as ElympicsVar;
						if (value != null)
						{
							if (value.EnabledSynchronization)
								_backingFields.Add(value);
						}
						else
							Debug.LogError($"Cannot synchronize ElympicsVar {field.Name} in {field.DeclaringType}, because it's null");
					}
				}
			}
		}

		internal byte[] GetState()
		{
			foreach (var synchronizable in _componentsContainer.SerializationHandlers)
				synchronizable.OnPreStateSerialize();

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				foreach (var backingField in _backingFields)
					backingField.Serialize(bw);

				return ms.ToArray();
			}
		}

		internal void ApplyState(byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
			{
				foreach (var backingField in _backingFields)
					backingField.Deserialize(br);
			}

			foreach (var synchronizable in _componentsContainer.SerializationHandlers)
				synchronizable.OnPostStateDeserialize();
		}

		internal bool AreStatesEqual(byte[] data1, byte[] data2)
		{
			using (var ms1 = new MemoryStream(data1))
			using (var br1 = new BinaryReader(ms1))
			using (var ms2 = new MemoryStream(data2))
			using (var br2 = new BinaryReader(ms2))
			{
				return _backingFields.All(backingField => backingField.Equals(br1, br2));
				// foreach (var backingField in _backingFields)
				// {
				// 	if (!backingField.Equals(br1, br2))
				// 	{
				// 		Debug.LogWarning($"Not equal on {backingField.GetType().Name}");
				// 		return false;
				// 	}
				// }
				//
				// return true;
			}
		}

		internal void GetInputForClient(BinaryInputWriter inputWriter)
		{
			foreach (var handler in _componentsContainer.InputHandlers)
				handler.GetInputForClient(inputWriter);
		}

		internal void GetInputForBot(BinaryInputWriter inputWriter)
		{
			foreach (var handler in _componentsContainer.InputHandlers)
				handler.GetInputForBot(inputWriter);
		}

		internal void ApplyInput(int playerId, BinaryInputReader inputReader)
		{
			foreach (var handler in _componentsContainer.InputHandlers)
				handler.ApplyInput(playerId, inputReader);
		}

		internal void PredictableFixedUpdate()
		{
			foreach (var predictable in _componentsContainer.Updatables)
				predictable.ElympicsUpdate();
		}

		internal void OnPreReconcile()
		{
			foreach (var reconciliationHandler in _componentsContainer.ReconciliationHandlers)
				reconciliationHandler.OnPreReconcile();
		}

		internal void OnPostReconcile()
		{
			foreach (var reconciliationHandler in _componentsContainer.ReconciliationHandlers)
				reconciliationHandler.OnPostReconcile();
		}

		#region ClientCallbacks

		public void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnStandaloneClientInit(data);
		}

		public void OnClientsOnServerInit(InitialMatchPlayerDatas data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnClientsOnServerInit(data);
		}

		public void OnConnected(TimeSynchronizationData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnConnected(data);
		}

		public void OnConnectingFailed()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnConnectingFailed();
		}

		public void OnDisconnectedByServer()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnDisconnectedByServer();
		}

		public void OnDisconnectedByClient()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnDisconnectedByClient();
		}

		public void OnSynchronized(TimeSynchronizationData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnSynchronized(data);
		}

		public void OnAuthenticated(string userId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnAuthenticated(userId);
		}

		public void OnAuthenticatedFailed(string errorMessage)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnAuthenticatedFailed(errorMessage);
		}

		public void OnMatchJoined(string matchId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchJoined(matchId);
		}

		public void OnMatchJoinedFailed(string errorMessage)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchJoinedFailed(errorMessage);
		}

		public void OnMatchEnded(string matchId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchEnded(matchId);
		}

		#endregion

		#region BotCallbacks

		public void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData)
		{
			foreach (var handler in _componentsContainer.BotHandlers)
				handler.OnStandaloneBotInit(initialMatchData);
		}

		public void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchDatas)
		{
			foreach (var handler in _componentsContainer.BotHandlers)
				handler.OnBotsOnServerInit(initialMatchDatas);
		}

		#endregion

		#region ServerCallbacks

		public void OnServerInit(InitialMatchPlayerDatas initialMatchData)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnServerInit(initialMatchData);
		}

		public void OnPlayerConnected(int playerId)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnPlayerConnected(playerId);
		}

		public void OnPlayerDisconnected(int playerId)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnPlayerDisconnected(playerId);
		}

		#endregion
	}
}
