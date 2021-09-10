using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Daftmobile.Api;
using LobbyPublicApiClients.User;
using LobbyPublicApiModels.User.Matchmaking;

namespace Elympics
{
	public class RemoteMatchmakerClient : IMatchmakerClient
	{
		private readonly IUserApiClient _userApiClient;

		public event Action<(string GameId, string GameVersion)>                 WaitingForMatchStarted;
		public event Action<(string GameId, string GameVersion, string MatchId)> WaitingForMatchFinished;
		public event Action<(string GameId, string GameVersion)>                 WaitingForMatchRetried;
		public event Action<(string GameId, string GameVersion, string Error)>   WaitingForMatchError;
		public event Action<(string GameId, string GameVersion)>                 WaitingForMatchCancelled;

		public event Action<string>                         WaitingForMatchStateInitializingStartedWithMatchId;
		public event Action<string>                         WaitingForMatchStateInitializingFinishedWithMatchId;
		public event Action<string>                         WaitingForMatchStateInitializingRetriedWithMatchId;
		public event Action<(string MatchId, string Error)> WaitingForMatchStateInitializingError;
		public event Action<string>                         WaitingForMatchStateInitializingCancelledWithMatchId;

		public event Action<string>                                                              WaitingForMatchStateRunningStartedWithMatchId;
		public event Action<(string MatchId, string ServerAddress, List<string> MatchedPlayers)> WaitingForMatchStateRunningFinished;
		public event Action<string>                                                              WaitingForMatchStateRunningRetriedWithMatchId;
		public event Action<(string MatchId, string Error)>                                      WaitingForMatchStateRunningError;
		public event Action<string>                                                              WaitingForMatchStateRunningCancelledWithMatchId;


		public event Action                                                                                         MatchmakingStarted;
		public event Action<(string MatchId, string ServerAddress, string UserSecret, List<string> MatchedPlayers)> MatchmakingFinished;
		public event Action                                                                                         MatchmakingCancelled;

		public RemoteMatchmakerClient(IUserApiClient userApiClient)
		{
			_userApiClient = userApiClient;
		}

		public async Task<bool> JoinMatchmakerAsync(string gameId, string gameVersion, bool tryReconnect, float[] matchmakerData = null, byte[] gameEngineData = null, CancellationToken ct = default)
		{
			MatchmakingStarted?.Invoke();
			using (ct.Register(() => MatchmakingCancelled?.Invoke()))
			{
				string matchId = null;
				if (tryReconnect)
					matchId = await GetFirstUnfinishedMatchId();

				if (matchId == null)
				{
					var waitForMatchResponse = await WaitForMatch(gameId, gameVersion, matchmakerData, gameEngineData, ct);
					if (!IsRequestSuccessful(waitForMatchResponse))
						return false;

					matchId = waitForMatchResponse.MatchId;
				}

				var waitForMatchInitializing = await WaitForMatchState(matchId, GetMatchDesiredState.Initializing, ct);
				if (!IsRequestSuccessful(waitForMatchInitializing))
					return false;

				var waitForMatchRunning = await WaitForMatchState(matchId, GetMatchDesiredState.Running, ct);
				if (!IsRequestSuccessful(waitForMatchRunning))
					return false;

				MatchmakingFinished?.Invoke((waitForMatchRunning.MatchId, waitForMatchRunning.ServerAddress, waitForMatchRunning.UserSecret, waitForMatchRunning.MatchedPlayersId));
				return true;
			}
		}

		private async Task<string> GetFirstUnfinishedMatchId()
		{
			var matchesIds = await GetUnfinishedMatchesIds();
			return matchesIds.Count > 0 ? matchesIds[0] : null;
		}

		private async Task<List<string>> GetUnfinishedMatchesIds()
		{
			var unfinishedMatchesResponse = await _userApiClient.GetUnfinishedMatchesAsync(new GetUnfinishedMatchesModel.Request());
			return unfinishedMatchesResponse.MatchesIds;
		}

		private bool IsRequestSuccessful(ApiResponse response) => response != null && response.IsSuccess;

		private async Task<JoinMatchmakerAndWaitForMatchModel.Response> WaitForMatch(string gameId, string gameVersion, float[] matchmakerData = null, byte[] gameEngineData = null, CancellationToken ct = default)
		{
			JoinMatchmakerAndWaitForMatchModel.Response joinMatchmakerResult = null;
			var getPendingMatchRequest = new JoinMatchmakerAndWaitForMatchModel.Request
			{
				GameId = gameId,
				GameVersion = gameVersion,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData
			};

			WaitingForMatchStarted?.Invoke((gameId, gameVersion));

			while (!ct.IsCancellationRequested)
			{
				try
				{
					joinMatchmakerResult = await _userApiClient.JoinMatchmakerAndWaitForMatch(getPendingMatchRequest, ct);

					if (IsOpponentFound(joinMatchmakerResult))
					{
						WaitingForMatchFinished?.Invoke((gameId, gameVersion, joinMatchmakerResult.MatchId));
						return joinMatchmakerResult;
					}
					else if (IsOpponentNotFound(joinMatchmakerResult))
					{
						WaitingForMatchRetried?.Invoke((gameId, gameVersion));
					}
					else
					{
						WaitingForMatchError?.Invoke((gameId, gameVersion, joinMatchmakerResult == null ? "null response" : joinMatchmakerResult.ErrorMessage));
						return joinMatchmakerResult;
					}
				}
				catch (TaskCanceledException)
				{
					WaitingForMatchCancelled?.Invoke((gameId, gameVersion));
					break;
				}
			}

			return joinMatchmakerResult;
		}

		private bool IsOpponentFound(JoinMatchmakerAndWaitForMatchModel.Response response)
		{
			return response != null && response.IsSuccess;
		}

		private static bool IsOpponentNotFound(JoinMatchmakerAndWaitForMatchModel.Response response)
		{
			return response != null && !response.IsSuccess && response.ErrorMessage == JoinMatchmakerAndWaitForMatchModel.ErrorCodes.OpponentNotFound;
		}

		private async Task<GetMatchModel.Response> WaitForMatchState(string matchId, GetMatchDesiredState desiredState, CancellationToken ct)
		{
			switch (desiredState)
			{
				case GetMatchDesiredState.Initializing:
					WaitingForMatchStateInitializingStartedWithMatchId?.Invoke(matchId);
					break;
				case GetMatchDesiredState.Running:
					WaitingForMatchStateRunningStartedWithMatchId?.Invoke(matchId);
					break;
			}

			GetMatchModel.Response getMatchResponse = null;
			var getMatchRequest = new GetMatchModel.Request
			{
				MatchId = matchId,
				DesiredState = desiredState
			};
			while (!ct.IsCancellationRequested)
			{
				try
				{
					getMatchResponse = await _userApiClient.GetMatchLongPolling(getMatchRequest, ct);
					if (IsMatchInDesiredState(getMatchResponse))
					{
						switch (desiredState)
						{
							case GetMatchDesiredState.Initializing:
								WaitingForMatchStateInitializingFinishedWithMatchId?.Invoke(matchId);
								break;
							case GetMatchDesiredState.Running:
								WaitingForMatchStateRunningFinished?.Invoke((matchId, getMatchResponse.ServerAddress, getMatchResponse.MatchedPlayersId));
								break;
						}

						return getMatchResponse;
					}
					else if (IsMatchNotInDesiredState(getMatchResponse))
					{
						switch (desiredState)
						{
							case GetMatchDesiredState.Initializing:
								WaitingForMatchStateInitializingRetriedWithMatchId?.Invoke(matchId);
								break;
							case GetMatchDesiredState.Running:
								WaitingForMatchStateRunningRetriedWithMatchId?.Invoke(matchId);
								break;
						}
					}
					else
					{
						switch (desiredState)
						{
							case GetMatchDesiredState.Initializing:
								WaitingForMatchStateInitializingError?.Invoke((matchId, getMatchResponse == null ? "null response" : getMatchResponse.ErrorMessage));
								break;
							case GetMatchDesiredState.Running:
								WaitingForMatchStateRunningError?.Invoke((matchId, getMatchResponse == null ? "null response" : getMatchResponse.ErrorMessage));
								break;
						}

						return getMatchResponse;
					}
				}
				catch (TaskCanceledException)
				{
					switch (desiredState)
					{
						case GetMatchDesiredState.Initializing:
							WaitingForMatchStateInitializingCancelledWithMatchId?.Invoke(matchId);
							break;
						case GetMatchDesiredState.Running:
							WaitingForMatchStateRunningCancelledWithMatchId?.Invoke(matchId);
							break;
					}

					break;
				}
			}

			return getMatchResponse;
		}

		private bool IsMatchInDesiredState(GetMatchModel.Response response)
		{
			return response != null && response.IsSuccess;
		}

		private bool IsMatchNotInDesiredState(GetMatchModel.Response response)
		{
			return response != null && !response.IsSuccess && response.ErrorMessage == GetMatchModel.ErrorCodes.NotInDesiredState;
		}
	}
}
