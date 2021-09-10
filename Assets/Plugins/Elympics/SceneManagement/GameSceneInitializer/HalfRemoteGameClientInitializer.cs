using System;

namespace Elympics
{
	internal class HalfRemoteGameClientInitializer : GameClientInitializer
	{
		private const string ElympicsHalfRemotePlayerIdEnvironmentVariable = "ELYMPICS_HALF_REMOTE_PLAYER_ID";

		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			var playerId = elympicsGameConfig.PlayerIdForHalfRemoteMode;
			if (IsHalfRemotePlayerIdEnvironmentVariableDefined())
				playerId = GetHalfRemotePlayerIdEnvironmentVariable();
			if (TryGetPlayerIdFromCommandLineArguments(out var argsPlayerId))
				playerId = argsPlayerId;

			var playersList = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig);

			var userId = playersList[playerId].UserId;
			var matchmakerData = playersList[playerId].MatchmakerData;
			var gameEngineData = playersList[playerId].GameEngineData;

			var halfRemoteMatchClient = new HalfRemoteMatchClientAdapter(elympicsGameConfig);
			var halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(halfRemoteMatchClient, elympicsGameConfig.IpForHalfRemoteMode, elympicsGameConfig.PortForHalfRemoteMode, userId);
			client.InitializeInternal(elympicsGameConfig, halfRemoteMatchConnectClient, halfRemoteMatchClient, new InitialMatchPlayerData
			{
				PlayerId = playerId,
				UserId = userId,
				IsBot = false,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData
			});
		}

		private static bool TryGetPlayerIdFromCommandLineArguments(out int argsPlayerId)
		{
			argsPlayerId = 0;
			var args = Environment.GetCommandLineArgs();
			return args.Length > 1 && int.TryParse(args[1], out argsPlayerId);
		}

		private static bool IsHalfRemotePlayerIdEnvironmentVariableDefined() => Environment.GetEnvironmentVariables().Contains(ElympicsHalfRemotePlayerIdEnvironmentVariable);
		private static int  GetHalfRemotePlayerIdEnvironmentVariable()       => int.Parse(Environment.GetEnvironmentVariable(ElympicsHalfRemotePlayerIdEnvironmentVariable) ?? string.Empty);
	}
}
