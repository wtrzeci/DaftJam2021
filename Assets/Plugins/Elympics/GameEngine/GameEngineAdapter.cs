using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._1;
using GameEngineCore.V1._3;
using UnityEngine;
using IGameEngine = GameEngineCore.V1._3.IGameEngine;

namespace Elympics
{
	public class GameEngineAdapter : IGameEngine
	{
		internal List<string> Players           { get; private set; } = new List<string>();
		internal int[]        PlayerIds         { get; private set; } = new int[0];

		public event Action<byte[], string>       InGameDataForPlayerOnReliableChannelGenerated;
		public event Action<byte[], string>       InGameDataForPlayerOnUnreliableChannelGenerated;
		public event Action<byte[]>               InGameDataForSpectatorsOnReliableChannelGenerated;
		public event Action<byte[]>               InGameDataForSpectatorsOnUnreliableChannelGenerated;
		public event Action                       GameStarted;
		public event Action<ResultMatchUserDatas> GameEnded;
		public event Action<int>                  PlayerConnected;
		public event Action<int>                  PlayerDisconnected;

		public event Action<InitialMatchPlayerDatas> InitializedWithMatchPlayerDatas;

		private IGameEngineLogger       _logger;
		private InitialMatchUserDatas   _initialMatchUserDatas;
		private Dictionary<string, int> _userIdToPlayerId;

		private readonly int _playerInputBufferSize;

		public ConcurrentDictionary<int, ElympicsDataWithTickBuffer<ElympicsInput>> PlayerInputBuffers { get; } =
			new ConcurrentDictionary<int, ElympicsDataWithTickBuffer<ElympicsInput>>();

		public GameEngineAdapter(ElympicsGameConfig elympicsGameConfig)
		{
			_playerInputBufferSize = elympicsGameConfig.PredictionBufferSize;
		}

		public void Init(IGameEngineLogger logger, InitialMatchData initialMatchData)
		{
			_logger = logger;
			Application.logMessageReceived += OnLogMessageReceived;
		}

		private void OnLogMessageReceived(string condition, string trace, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					_logger.Error(condition);
					break;
				case LogType.Assert:
					_logger.Fatal(condition);
					break;
				case LogType.Warning:
					_logger.Warning(condition);
					break;
				case LogType.Log:
					_logger.Info(condition);
					break;
				case LogType.Exception:
					_logger.Error(condition);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		public void Init2(InitialMatchUserDatas initialMatchUserDatas)
		{
			Players = initialMatchUserDatas.Select(x => x.UserId).ToList();
			PlayerIds = Enumerable.Range(0, initialMatchUserDatas.Count).Select(ElympicsPlayer.GetPlayerId).ToArray();

			_userIdToPlayerId = ElympicsPlayerAssociations.GetPlayerAssociations(Players);
			foreach (var userId in initialMatchUserDatas.Select(userData => userData.UserId))
				PlayerInputBuffers[_userIdToPlayerId[userId]] = new ElympicsDataWithTickBuffer<ElympicsInput>(_playerInputBufferSize);

			_initialMatchUserDatas = initialMatchUserDatas;
			InitializedWithMatchPlayerDatas?.Invoke(new InitialMatchPlayerDatas(initialMatchUserDatas.Select(x => new InitialMatchPlayerData
			{
				PlayerId = _userIdToPlayerId[x.UserId],
				UserId = x.UserId,
				IsBot = x.IsBot,
				BotDifficulty = x.BotDifficulty,
				GameEngineData = x.GameEngineData,
				MatchmakerData = x.MatchmakerData
			}).ToList()));
			_logger.Info("Initialized from unity");
		}

		public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId)   => AddReliableInputToBuffer(data, userId);
		public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId) => AddUnreliableInputsToBuffer(data, userId);

		private void AddReliableInputToBuffer(byte[] data, string userId)
		{
			var playerId = _userIdToPlayerId[userId];
			var input = ElympicsInputSerializer.Deserialize(data);
			AddInputToBuffer(input, playerId);
		}

		private void AddUnreliableInputsToBuffer(byte[] data, string userId)
		{
			var playerId = _userIdToPlayerId[userId];
			var inputs = ElympicsInputSerializer.DeserializePackage(data);

			foreach (var input in inputs)
				AddInputToBuffer(input, playerId);
		}

		private void AddInputToBuffer(ElympicsInput input, int playerId)
		{
			input.PlayerId = playerId;
			if (!PlayerInputBuffers.TryGetValue(playerId, out var buffer))
			{
				Debug.LogWarning($"Input buffer for {playerId} not found");
				return;
			}

			buffer.TryAddData(input);
		}

		internal void AddBotsOrClientsInServerInputToBuffer(ElympicsInput input, int playerId) => AddInputToBuffer(input, playerId);

		public void OnPlayerConnected(string userId)    => PlayerConnected?.Invoke(_userIdToPlayerId[userId]);
		public void OnPlayerDisconnected(string userId) => PlayerDisconnected?.Invoke(_userIdToPlayerId[userId]);

		public void Tick(long tick)
		{
			// _logger.Info($"Hello from unity tick {tick}");
			/* Using unity FixedUpdate */
		}

		public void SendSnapshotUnreliable(ElympicsSnapshot snapshot)
		{
			var serializedData = snapshot.Serialize();
			foreach (var userData in _initialMatchUserDatas)
				InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(serializedData, userData.UserId);
		}

		public void SendSnapshotsUnreliable(Dictionary<int, ElympicsSnapshot> snapshots)
		{
			foreach (var (playerId, snapshot) in snapshots)
			{
				var userId = Players[playerId];
				var serializedData = snapshot.Serialize();
				InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(serializedData, userId);
			}
		}

		public void StartGame() => GameStarted?.Invoke();

		public void EndGame(ResultMatchPlayerDatas result = null)
		{
			try
			{
				if (result == null)
				{
					GameEnded?.Invoke(null);
					return;
				}

				if (result.Count != _initialMatchUserDatas.Count)
				{
					_logger.Error("Invalid length of match result, expected {0}, has {1}", _initialMatchUserDatas.Count, result.Count);
					GameEnded?.Invoke(null);
					return;
				}

				var matchResult = new ResultMatchUserDatas();
				for (var i = 0; i < result.Count; i++)
				{
					var userId = Players[i];
					matchResult.Add(new ResultMatchUserData
					{
						UserId = userId,
						GameEngineData = result[i].GameEngineData,
						MatchmakerData = result[i].MatchmakerData
					});
				}

				GameEnded?.Invoke(matchResult);
			}
			finally
			{
				Application.Quit(result == null ? 1 : 0);
			}
		}

		public event Action<List<GameEvent>> GameEventsGathered;

		event Action<GameEngineCore.V1._1.MatchResult> GameEngineCore.V1._1.IGameEngine.GameEnded
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}
	}
}
