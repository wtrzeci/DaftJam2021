using System.Linq;
using System.Threading.Tasks;
using GameBotCore.V1._3;

namespace Elympics
{
	internal class HalfRemoteGameBotInitializer : GameBotInitializer
	{
		private HalfRemoteMatchConnectClient _halfRemoteMatchConnectClient;

		protected override void InitializeBot(ElympicsBot bot, ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter)
		{
			var playerId = elympicsGameConfig.PlayerIdForHalfRemoteMode;
			var playersList = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig);
			var userId = playersList[playerId].UserId;

			var botConfiguration = new BotConfiguration
			{
				Difficulty = 0,
				UserId = userId,
				MatchPlayers = playersList.Select(x => x.UserId).ToList(),
				MatchId = null,
				MatchmakerData = playersList[playerId].MatchmakerData,
				GameEngineData = playersList[playerId].GameEngineData,
			};

			var halfRemoteMatchClient = new HalfRemoteMatchClientAdapter(elympicsGameConfig);
			_halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(halfRemoteMatchClient, elympicsGameConfig.IpForHalfRemoteMode, elympicsGameConfig.PortForHalfRemoteMode, userId);

			halfRemoteMatchClient.RawSnapshotReceived += gameBotAdapter.OnInGameDataUnreliableReceived;
			gameBotAdapter.InGameDataForReliableChannelGenerated += data => halfRemoteMatchClient.SendRawInputReliable(data);
			gameBotAdapter.InGameDataForUnreliableChannelGenerated += data => halfRemoteMatchClient.SendRawInputUnreliable(data);
			
			gameBotAdapter.Init(new LoggerNoop(), null);
			gameBotAdapter.Init2(null);
			gameBotAdapter.Init3(botConfiguration);
			
			Task.Factory.StartNew(_halfRemoteMatchConnectClient.ConnectAndJoinAsPlayer);
		}

		public override void Dispose()
		{
			_halfRemoteMatchConnectClient?.Disconnect();
		}
	}
}
