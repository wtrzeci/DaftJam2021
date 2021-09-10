using System;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	public class ElympicsClient : ElympicsBase
	{
		[SerializeField] private bool connectOnStart = true;

		private         int  _playerId;
		public override int  PlayerId => _playerId;
		public override bool IsClient => true;

		public  bool Initialized { get; private set; }
		private bool _started;

		public IMatchConnectClient MatchConnectClient
		{
			get
			{
				if (_matchConnectClient == null)
					throw new Exception("Elympics not initialized! Did you change ScriptExecutionOrder?");
				return _matchConnectClient;
			}
		}

		private IMatchConnectClient _matchConnectClient;
		private IMatchClient        _matchClient;

		// Prediction
		private IRoundTripTimeCalculator _roundTripTimeCalculator;
		private ClientTickCalculator     _clientTickCalculator;
		private PredictionBuffer         _predictionBuffer;

		private ElympicsSnapshot _lastReceivedSnapshot;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, IMatchConnectClient matchConnectClient, IMatchClient matchClient, InitialMatchPlayerData initialMatchPlayerData)
		{
			base.InitializeInternal(elympicsGameConfig);
			_playerId = initialMatchPlayerData.PlayerId;
			_matchConnectClient = matchConnectClient;
			_matchClient = matchClient;

			elympicsBehavioursManager.InitializeInternal(this);
			_roundTripTimeCalculator = new RoundTripTimeCalculator(_matchClient, _matchConnectClient);
			_clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator);
			_predictionBuffer = new PredictionBuffer(elympicsGameConfig);

			SetupCallbacks();
			OnStandaloneClientInit(initialMatchPlayerData);

			Initialized = true;
			if (connectOnStart)
				Task.Factory.StartNew(ConnectAndJoinAsPlayer);
		}

		private void SetupCallbacks()
		{
			_matchClient.SnapshotReceived += OnSnapshotReceived;
			_matchClient.Synchronized += OnSynchronized;
			_matchConnectClient.DisconnectedByServer += OnDisconnectedByServer;
			_matchConnectClient.DisconnectedByClient += OnDisconnectedByClient;
			_matchConnectClient.ConnectedWithSynchronizationData += OnConnectedWithSynchronizationData;
			_matchConnectClient.ConnectingFailed += OnConnectingFailed;
			_matchConnectClient.AuthenticatedUserMatchWithUserId += OnAuthenticated;
			_matchConnectClient.AuthenticatedUserMatchFailedWithError += OnAuthenticatedFailed;
			_matchConnectClient.MatchJoinedWithMatchId += OnMatchJoined;
			_matchConnectClient.MatchJoinedWithError += OnMatchJoinedFailed;
			_matchConnectClient.MatchEndedWithMatchId += OnMatchEnded;
		}

		private void OnConnectedWithSynchronizationData(TimeSynchronizationData data)
		{
			_roundTripTimeCalculator.OnSynchronized(data);
			OnConnected(data);
		}

		private void OnDestroy()
		{
			if (_matchClient != null)
				_matchClient.SnapshotReceived -= OnSnapshotReceived;
		}

		private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
		{
			if (!_started) StartClient();

			if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
				_lastReceivedSnapshot = elympicsSnapshot;
		}

		private void StartClient() => _started = true;

		protected override bool ShouldDoFixedUpdate() => Initialized && _started;

		protected override void DoFixedUpdate()
		{
			var receivedSnapshot = _lastReceivedSnapshot;
			_clientTickCalculator.CalculateNextTick(receivedSnapshot.Tick);
			_predictionBuffer.UpdateMinTick(receivedSnapshot.Tick);

			ProcessInput();

			if (Config.Prediction)
			{
				ReconcileIfRequired(receivedSnapshot);
				ApplyUnpredictablePartOfSnapshot(receivedSnapshot);
				ApplyPredictedInput();
			}
			else
			{
				ApplyFullSnapshot(receivedSnapshot);
			}
		}

		protected override void LateFixedUpdate() => ProcessSnapshot();

		private void ProcessSnapshot()
		{
			if (Config.Prediction)
			{
				var snapshot = elympicsBehavioursManager.GetSnapshot();
				snapshot.Tick = _clientTickCalculator.PredictionTick;
				_predictionBuffer.AddSnapshotToBuffer(snapshot);
			}
		}

		private void ProcessInput()
		{
			var input = elympicsBehavioursManager.GetInputForClient();
			AddMetadataToInput(input);
			SendInput(input);
			_predictionBuffer.AddInputToBuffer(input);
		}

		private void AddMetadataToInput(ElympicsInput input)
		{
			input.Tick = (int) _clientTickCalculator.DelayedInputTick;
			input.PlayerId = PlayerId;
		}

		private void SendInput(ElympicsInput input) => _matchClient.SendInputUnreliable(input);

		private void ApplyPredictedInput()
		{
			if (_predictionBuffer.TryGetInputFromBuffer(_clientTickCalculator.PredictionTick, out var predictedInput))
				elympicsBehavioursManager.ApplyInput(predictedInput);
		}

		private void ApplyUnpredictablePartOfSnapshot(ElympicsSnapshot snapshot)
			=> elympicsBehavioursManager.ApplySnapshot(snapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

		private void ReconcileIfRequired(ElympicsSnapshot receivedSnapshot)
		{
			if (Config.ReconciliationFrequency == ElympicsGameConfig.ReconciliationFrequencyEnum.Never)
				return;

			if (!_predictionBuffer.TryGetSnapshotFromBuffer(receivedSnapshot.Tick, out var historySnapshot))
				return;

			if (elympicsBehavioursManager.AreSnapshotsEqualOnPredictableBehaviours(historySnapshot, receivedSnapshot) &&
			    Config.ReconciliationFrequency != ElympicsGameConfig.ReconciliationFrequencyEnum.OnEverySnapshot)
				return;

			Debug.LogWarning($"[Player {PlayerId}] Reconciliation on {receivedSnapshot.Tick}");

			elympicsBehavioursManager.OnPreReconcile();

			// Debug.Log($"[{_playerId}] Applying snapshot {_lastReceivedSnapshot.Tick} with {JsonConvert.SerializeObject(_lastReceivedSnapshot, Formatting.Indented)}");
			elympicsBehavioursManager.ApplySnapshot(receivedSnapshot, ElympicsBehavioursManager.StatePredictability.Predictable);
			elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

			var currentSnapshot = elympicsBehavioursManager.GetSnapshot();
			currentSnapshot.Tick = receivedSnapshot.Tick;
			_predictionBuffer.AddOrReplaceSnapshotInBuffer(currentSnapshot);

			for (var resimulationTick = receivedSnapshot.Tick + 1; resimulationTick < _clientTickCalculator.PredictionTick; resimulationTick++)
			{
				if (_predictionBuffer.TryGetSnapshotFromBuffer(resimulationTick, out historySnapshot))
					elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

				if (_predictionBuffer.TryGetInputFromBuffer(resimulationTick, out var resimulatedInput))
					elympicsBehavioursManager.ApplyInput(resimulatedInput);
				elympicsBehavioursManager.PredictableFixedUpdate();

				var newResimulatedSnapshot = elympicsBehavioursManager.GetSnapshot();
				newResimulatedSnapshot.Tick = resimulationTick;
				_predictionBuffer.AddOrReplaceSnapshotInBuffer(newResimulatedSnapshot);
				// Debug.Log($"[{PlayerId}] Overriding snapshot {resimulationTick} with {JsonConvert.SerializeObject(newResimulatedSnapshot, Formatting.Indented)}");
			}

			elympicsBehavioursManager.OnPostReconcile();
		}

		private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot) => elympicsBehavioursManager.ApplySnapshot(receivedSnapshot);

		#region IElympics

		public override async Task<bool> ConnectAndJoinAsPlayer()    => await MatchConnectClient.ConnectAndJoinAsPlayer();
		public override async Task<bool> ConnectAndJoinAsSpectator() => await MatchConnectClient.ConnectAndJoinAsSpectator();
		public override       void       Disconnect()                => MatchConnectClient.Disconnect();

		#endregion
	}
}
