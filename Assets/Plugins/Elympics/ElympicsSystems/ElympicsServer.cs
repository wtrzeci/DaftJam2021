using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
	public class ElympicsServer : ElympicsBase
	{
		private const int ServerPlayerId = ElympicsPlayer.WORLD_ID;

		private int  _currentPlayerId = ElympicsPlayer.INVALID_ID;
		private bool _currentIsBot;
		private bool _currentIsClient;

		public override int  PlayerId => _currentPlayerId;
		public override bool IsServer => true;
		public override bool IsBot    => _currentIsBot;
		public override bool IsClient => _currentIsClient;

		private bool HandlingBotsInServer    => Config.GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots || Config.BotsInServer;
		private bool HandlingClientsInServer => Config.GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots;

		private GameEngineAdapter _gameEngineAdapter;
		private int[]             _playerIdsOfBots;
		private int[]             _playerIdsOfClient;

		private bool _initialized;
		private int  _tick;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
		{
			SwitchBehaviourToServer();
			base.InitializeInternal(elympicsGameConfig);
			_gameEngineAdapter = gameEngineAdapter;
			_tick = 0;
			elympicsBehavioursManager.InitializeInternal(this);
			SetupCallbacks();
		}

		private void SetupCallbacks()
		{
			_gameEngineAdapter.PlayerConnected += OnPlayerConnected;
			_gameEngineAdapter.PlayerDisconnected += OnPlayerDisconnected;
			_gameEngineAdapter.InitializedWithMatchPlayerDatas += OnServerInit;
			_gameEngineAdapter.InitializedWithMatchPlayerDatas += InitializeBotsAndClientInServer;
			_gameEngineAdapter.InitializedWithMatchPlayerDatas += SetServerInitializedWithinAsyncQueue;
		}

		private void SetServerInitializedWithinAsyncQueue(InitialMatchPlayerDatas _) => Enqueue(() => _initialized = true);

		private void InitializeBotsAndClientInServer(InitialMatchPlayerDatas data)
		{
			if (HandlingBotsInServer)
			{
				var dataOfBots = data.Where(x => x.IsBot).ToList();
				OnBotsOnServerInit(new InitialMatchPlayerDatas(dataOfBots));

				_playerIdsOfBots = dataOfBots.Select(x => x.PlayerId).ToArray();
				CallPlayerConnectedFromBotsOrClients(_playerIdsOfBots);
			}

			if (HandlingClientsInServer)
			{
				var dataOfClients = data.Where(x => !x.IsBot).ToList();
				OnClientsOnServerInit(new InitialMatchPlayerDatas(dataOfClients));

				_playerIdsOfClient = dataOfClients.Select(x => x.PlayerId).ToArray();
				CallPlayerConnectedFromBotsOrClients(_playerIdsOfClient);
			}
		}


		private void CallPlayerConnectedFromBotsOrClients(int[] playerIds)
		{
			foreach (var playerId in playerIds)
				OnPlayerConnected(playerId);
		}

		protected override bool ShouldDoFixedUpdate() => _initialized;

		protected override void DoFixedUpdate()
		{
			if (HandlingBotsInServer)
				GatherInputsFromServerBotsOrClient(_playerIdsOfBots, SwitchBehaviourToBot, BotInputGetter);
			if (HandlingClientsInServer)
				GatherInputsFromServerBotsOrClient(_playerIdsOfClient, SwitchBehaviourToClient, ClientInputGetter);

			foreach (var inputBufferPair in _gameEngineAdapter.PlayerInputBuffers)
				if (inputBufferPair.Value.TryGetDataForTick(_tick, out var input))
					elympicsBehavioursManager.ApplyInput(input);
		}

		private static ElympicsInput ClientInputGetter(ElympicsBehavioursManager manager) => manager.GetInputForClient();
		private static ElympicsInput BotInputGetter(ElympicsBehavioursManager manager)    => manager.GetInputForBot();

		private void GatherInputsFromServerBotsOrClient(int[] playerIds, Action<int> switchElympicsBaseBehaviour, Func<ElympicsBehavioursManager, ElympicsInput> getInput)
		{
			foreach (var playerIdOfBotOrClient in playerIds)
			{
				switchElympicsBaseBehaviour(playerIdOfBotOrClient);
				var input = getInput(elympicsBehavioursManager);
				input.Tick = _tick;
				input.PlayerId = playerIdOfBotOrClient;
				_gameEngineAdapter.AddBotsOrClientsInServerInputToBuffer(input, playerIdOfBotOrClient);
			}

			SwitchBehaviourToServer();
		}

		protected override void LateFixedUpdate()
		{
			if (ShouldSendSnapshot(_tick))
			{
				// gather state info from scene and send to clients
				var snapshots = elympicsBehavioursManager.GetSnapshots(_gameEngineAdapter.PlayerIds);
				AddMetadataToSnapshots(snapshots, _tick);
				_gameEngineAdapter.SendSnapshotsUnreliable(snapshots);
			}

			_tick++;

			foreach (var (_, inputBuffer) in _gameEngineAdapter.PlayerInputBuffers)
				inputBuffer.UpdateMinTick(_tick);
		}

		private bool ShouldSendSnapshot(int tick) => tick % Config.SnapshotSendingPeriodInTicks == 0;

		private void AddMetadataToSnapshots(Dictionary<int, ElympicsSnapshot> snapshots, int tick)
		{
			foreach (var (_, snapshot) in snapshots)
				snapshot.Tick = tick;
		}

		private void SwitchBehaviourToServer()
		{
			_currentPlayerId = ServerPlayerId;
			_currentIsClient = false;
			_currentIsBot = false;
		}

		private void SwitchBehaviourToBot(int playerId)
		{
			_currentPlayerId = playerId;
			_currentIsClient = false;
			_currentIsBot = true;
		}

		private void SwitchBehaviourToClient(int playerId)
		{
			_currentPlayerId = playerId;
			_currentIsClient = true;
			_currentIsBot = false;
		}

		#region IElympics

		public override void StartGame()                                   => _gameEngineAdapter.StartGame();
		public override void EndGame(ResultMatchPlayerDatas result = null) => _gameEngineAdapter.EndGame(result);

		#endregion
	}
}
