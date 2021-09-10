using System;
using System.Collections.Generic;
using System.Threading;
using LobbyPublicApiClients.User;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
	public class ElympicsLobbyClient : MonoBehaviour
	{
		private const string AUTH_TOKEN_PLAYER_PREFS_KEY = "Elympics/AuthToken";

		public static ElympicsLobbyClient Instance { get; private set; }

#pragma warning disable IDE0044 // Add readonly modifier
		[SerializeField] private bool authenticateOnAwake = true;
#pragma warning restore IDE0044 // Add readonly modifier

		public delegate void AuthenticationCallback(bool success, string userId, string jwtToken);

		public event AuthenticationCallback Authenticated;

		public string          UserId          { get; private set; }
		public bool            IsAuthenticated { get; private set; }
		public JoinedMatchData MatchData       { get; private set; }

		private ElympicsGameConfig _config;
		private Action             _authenticationSuccessCallback;
		private UserApiClient      _lobbyPublicApiClient;

		private string  _authToken;
		private float[] _matchmakerData;
		private byte[]  _gameEngineData;

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
			SetAuthToken();
			LoadConfig();
			if (authenticateOnAwake)
				Authenticate();
		}

		private void LoadConfig()
		{
			_config = ElympicsConfig.LoadCurrentElympicsGameConfig();
		}

		public void Authenticate(Action onSuccess = null)
		{
			if (IsAuthenticated)
			{
				Debug.LogError("[Elympics] User already authenticated.");
				return;
			}

			string authenticationEndpoint = _config.ElympicsEndpoint;
			if (string.IsNullOrEmpty(authenticationEndpoint))
			{
				Debug.LogError($"[Elympics] Elympics authentication endpoint not set. Finish configuration using [{ElympicsTools.SETUP_MENU_PATH}]");
				return;
			}

			_authenticationSuccessCallback = onSuccess;
			_lobbyPublicApiClient = new UserApiClient(new LoggerDebug());
			var authClient = new RemoteAuthenticationClient(_lobbyPublicApiClient);
			StartCoroutine(CoroutineTaskCreator.RunTaskCoroutine(authClient.AuthenticateWithAuthToken(authenticationEndpoint, _authToken), OnAuthenticationSuccess, LogError));
		}

		public void JoinMatch(float[] matchmakerData = null, byte[] gameEngineData = null)
		{
			if (!IsAuthenticated)
			{
				Debug.LogError("[Elympics] User not authenticated, aborting join match.");
				return;
			}

			_matchmakerData = matchmakerData;
			_gameEngineData = gameEngineData;

			var matchMakerClient = new RemoteMatchmakerClient(_lobbyPublicApiClient);
			matchMakerClient.MatchmakingFinished += HandleMatchMakingFinished;
			StartCoroutine(CoroutineTaskCreator.RunTaskCoroutine(matchMakerClient.JoinMatchmakerAsync(_config.GameId, _config.GameVersion, _config.ReconnectEnabled, matchmakerData, gameEngineData, CancellationToken.None),
				OnMatchmakingFinished));
		}

		private void OnMatchmakingFinished(bool success)
		{
			if (success)
				Debug.Log("Matchmaking finished successfully");
			else
				Debug.Log("Matchmaking failed");
		}

		private void HandleMatchMakingFinished((string MatchId, string ServerAddress, string UserSecret, List<string> MatchedPlayers) obj)
		{
			MatchData = new JoinedMatchData(obj.MatchId, obj.ServerAddress, obj.UserSecret, obj.MatchedPlayers, _matchmakerData, _gameEngineData);
			SceneManager.LoadScene(_config.GameplayScene);
		}

		private void OnAuthenticationSuccess((bool Success, string UserId, string JwtToken) result)
		{
			Debug.Log("[Elympics] Authentication successful");
			IsAuthenticated = true;
			UserId = result.UserId;
			_authenticationSuccessCallback?.Invoke();
			Authenticated?.Invoke(result.Success, result.UserId, result.JwtToken);
		}

		private void LogError(string error) => Debug.LogError($"[Elympics] Authentication failed: {error}");

		private void SetAuthToken()
		{
			if (!PlayerPrefs.HasKey(AUTH_TOKEN_PLAYER_PREFS_KEY))
				CreateNewAuthToken();
			_authToken = PlayerPrefs.GetString(AUTH_TOKEN_PLAYER_PREFS_KEY);
		}

		private static void CreateNewAuthToken()
		{
			PlayerPrefs.SetString(AUTH_TOKEN_PLAYER_PREFS_KEY, Guid.NewGuid().ToString());
			PlayerPrefs.Save();
		}
	}
}