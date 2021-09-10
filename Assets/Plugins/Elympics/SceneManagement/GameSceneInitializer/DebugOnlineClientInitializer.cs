using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	internal class DebugOnlineClientInitializer : GameClientInitializer
	{
		private ElympicsClient _client;

		private RemoteAuthenticationClient _myAuthenticationClient;
		private RemoteMatchmakerClient     _myMatchmakerClient;

		private ElympicsGameConfig _elympicsGameConfig;

		private string _myUserId;

		private float[] _matchmakerData = null;
		private byte[]  _gameEngineData = null;

		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			_client = client;

			var loggerDebug = new LoggerDebug();
			var lobbyPublicApiClient = new LobbyPublicApiClients.User.UserApiClient(loggerDebug);
			_myAuthenticationClient = new RemoteAuthenticationClient(lobbyPublicApiClient);
			_myMatchmakerClient = new RemoteMatchmakerClient(lobbyPublicApiClient);

			_elympicsGameConfig = elympicsGameConfig;
			_ = Connect();
		}

		private async Task Connect()
		{
			try
			{
				var userSecret = Guid.NewGuid().ToString();

				var (success, userId, jwtToken) = await _myAuthenticationClient.AuthenticateWithAuthToken(_elympicsGameConfig.ElympicsEndpoint, userSecret);
				_myUserId = userId;

				if (!success)
				{
					Debug.LogError("Connecting failed");
					return;
				}

				_myMatchmakerClient.MatchmakingFinished += OnMatchmakingFinished;

				await _myMatchmakerClient.JoinMatchmakerAsync(_elympicsGameConfig.GameId, _elympicsGameConfig.GameVersion, false, _matchmakerData, _gameEngineData);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("{0} \n {1}", e.Message, e.StackTrace);
			}
		}

		private void OnMatchmakingFinished((string MatchId, string ServerAddress, string UserSecret, List<string> MatchedPlayers) matchData)
		{
			var playerId = ElympicsPlayerAssociations.GetPlayerAssociations(matchData.MatchedPlayers)[_myUserId];

			var gameServerClient = new GameServerClient(new LoggerDebug(), new ClientSynchronizerConfig
			{
				// Todo use config ~pprzestrzelski 11.03.2021
				TimeoutTime = TimeSpan.FromSeconds(5),
				ContinuousSynchronizationMinimumInterval = TimeSpan.FromSeconds(1)
			});
			var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.ServerAddress, matchData.UserSecret);
			var matchClient = new RemoteMatchClient(gameServerClient, _elympicsGameConfig);

			_client.InitializeInternal(_elympicsGameConfig, matchConnectClient, matchClient, new InitialMatchPlayerData
			{
				PlayerId = playerId,
				UserId = _myUserId,
				IsBot = false,
				MatchmakerData = _matchmakerData,
				GameEngineData = _gameEngineData
			});
		}
	}
}