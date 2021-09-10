using System;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	internal class OnlineGameClientInitializer : GameClientInitializer
	{
		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			var matchData = ElympicsLobbyClient.Instance.MatchData;
			if (matchData == null)
			{
				Debug.LogError("[Elympics] Match data not found. Did you try to join an online match without going through matchmaking first?");
				return;
			}

			var userId = ElympicsLobbyClient.Instance.UserId;
			var matchmakerData = ElympicsLobbyClient.Instance.MatchData.MatchmakerData;
			var gameEngineData = ElympicsLobbyClient.Instance.MatchData.GameEngineData;
			var playerId = ElympicsPlayerAssociations.GetPlayerAssociations(matchData.MatchedPlayers)[userId];

			var gameServerClient = new GameServerClient(new LoggerDebug(), new ClientSynchronizerConfig
			{
				// Todo use config ~pprzestrzelski 11.03.2021
				TimeoutTime = TimeSpan.FromSeconds(5),
				ContinuousSynchronizationMinimumInterval = TimeSpan.FromSeconds(1)
			});
			var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.ServerAddress, matchData.UserSecret);
			var matchClient = new RemoteMatchClient(gameServerClient, elympicsGameConfig);

			client.InitializeInternal(elympicsGameConfig, matchConnectClient, matchClient, new InitialMatchPlayerData
			{
				PlayerId = playerId,
				UserId = userId,
				IsBot = false,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData
			});
		}
	}
}
