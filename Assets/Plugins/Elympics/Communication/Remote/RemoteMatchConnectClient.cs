using System;
using System.Threading.Tasks;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;

namespace Elympics
{
	public class RemoteMatchConnectClient : IMatchConnectClient
	{
		public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
		public event Action                          ConnectingFailed;

		public event Action<string> AuthenticatedUserMatchWithUserId;
		public event Action<string> AuthenticatedUserMatchFailedWithError;

		public event Action         AuthenticatedAsSpectator;
		public event Action<string> AuthenticatedAsSpectatorWithError;

		public event Action<string> MatchJoinedWithError;
		public event Action<string> MatchJoinedWithMatchId;

		public event Action<string> MatchEndedWithMatchId;

		public event Action DisconnectedByServer;
		public event Action DisconnectedByClient;

		private readonly IGameServerClient _gameServerClient;

		private string _serverAddress;
		private string _userSecret;

		private          bool   _connecting     = false;
		private readonly object _connectingLock = new object();

		private          bool   _connected     = false;
		private readonly object _connectedLock = new object();

		private TaskCompletionSource<bool> _disconnectedTcs;
		private TaskCompletionSource<bool> _matchJoinedTcs;

		public RemoteMatchConnectClient(IGameServerClient gameServerClient, string serverAddress, string userSecret = null)
		{
			_gameServerClient = gameServerClient;
			_serverAddress = serverAddress;
			_userSecret = userSecret;
			_gameServerClient.Disconnected += OnDisconnectedByServer;
		}

		public async Task<bool> ConnectAndJoinAsPlayer()
		{
			if (string.IsNullOrEmpty(_serverAddress))
				throw new ArgumentNullException(nameof(_serverAddress));
			if (string.IsNullOrEmpty(_userSecret))
				throw new ArgumentNullException(nameof(_userSecret));
			return await ConnectAndJoin(SetupCallbacksForJoiningAsPlayer, UnsetCallbacksForJoiningAsPlayer);
		}

		public async Task<bool> ConnectAndJoinAsSpectator()
		{
			if (string.IsNullOrEmpty(_serverAddress))
				throw new ArgumentNullException(nameof(_serverAddress));
			return await ConnectAndJoin(SetupCallbacksForJoiningAsSpectator, UnsetCallbacksForJoiningAsSpectator);
		}

		public void Disconnect()
		{
			lock (_connectedLock)
			{
				if (!_connected)
					return;
				_connected = false;
			}
			DisconnectedByClient?.Invoke();
			_gameServerClient.Disconnect();
		}

		private void SetupCallbacksForJoiningAsPlayer()
		{
			_gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsPlayer;
			_gameServerClient.UserMatchAuthenticated += OnAuthenticatedMatchUserSecret;
			_gameServerClient.MatchJoined += OnMatchJoined;
			_gameServerClient.MatchEnded += OnMatchEnded;
			_gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
		}

		private void UnsetCallbacksForJoiningAsPlayer()
		{
			_gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsPlayer;
			_gameServerClient.UserMatchAuthenticated -= OnAuthenticatedMatchUserSecret;
			_gameServerClient.MatchJoined -= OnMatchJoined;
			_gameServerClient.MatchEnded -= OnMatchEnded;
			_gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
		}

		private void SetupCallbacksForJoiningAsSpectator()
		{
			_gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsSpectator;
			_gameServerClient.AuthenticatedAsSpectator += OnAuthenticatedAsSpectator;
			_gameServerClient.MatchJoined += OnMatchJoined;
			_gameServerClient.MatchEnded += OnMatchEnded;
			_gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
		}

		private void UnsetCallbacksForJoiningAsSpectator()
		{
			_gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsSpectator;
			_gameServerClient.AuthenticatedAsSpectator -= OnAuthenticatedAsSpectator;
			_gameServerClient.MatchJoined -= OnMatchJoined;
			_gameServerClient.MatchEnded -= OnMatchEnded;
			_gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
		}

		private async Task<bool> ConnectAndJoin(Action setupCallbacks, Action unsetCallbacks)
		{
			lock (_connectingLock)
			{
				if (_connecting)
					return false;
				_connecting = true;
			}

			lock (_connectedLock)
				if (_connected)
					return false;

			_disconnectedTcs = new TaskCompletionSource<bool>();
			_matchJoinedTcs = new TaskCompletionSource<bool>();

			try
			{
				setupCallbacks();

				var connected = await _gameServerClient.ConnectAsync(_serverAddress);
				if (!connected)
				{
					ConnectingFailed?.Invoke();
					return false;
				}

				var finishedTask = await Task.WhenAny(_disconnectedTcs.Task, _matchJoinedTcs.Task);
				var success = finishedTask == _matchJoinedTcs.Task;
				if (!success)
					return false;

				lock (_connectedLock)
					_connected = true;
				return true;
			}
			finally
			{
				unsetCallbacks();

				lock (_connectingLock)
					_connecting = false;
				TryDisconnectByServerIfNotConnected();
			}
		}

		private void OnConnectedAndSynchronizedAsPlayer(TimeSynchronizationData timeSynchronizationData)
		{
			ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
			_gameServerClient.AuthenticateMatchUserSecretAsync(_userSecret);
		}

		private void OnConnectedAndSynchronizedAsSpectator(TimeSynchronizationData timeSynchronizationData)
		{
			ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
			_gameServerClient.AuthenticateAsSpectatorAsync();
		}

		private void OnAuthenticatedMatchUserSecret(UserMatchAuthenticatedMessage message)
		{
			if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
			{
				AuthenticatedUserMatchFailedWithError?.Invoke(message.ErrorMessage);
				_gameServerClient.Disconnect();
				return;
			}

			AuthenticatedUserMatchWithUserId?.Invoke(message.UserId);

			_gameServerClient.JoinMatchAsync();
		}

		private void OnAuthenticatedAsSpectator(AuthenticatedAsSpectatorMessage message)
		{
			if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
			{
				AuthenticatedAsSpectatorWithError?.Invoke(message.ErrorMessage);
				_gameServerClient.Disconnect();
				return;
			}

			AuthenticatedAsSpectator?.Invoke();

			_gameServerClient.JoinMatchAsync();
		}

		private void OnMatchJoined(MatchJoinedMessage message)
		{
			if (!string.IsNullOrEmpty(message.ErrorMessage))
			{
				MatchJoinedWithError?.Invoke(message.ErrorMessage);
				_gameServerClient.Disconnect();
				return;
			}

			MatchJoinedWithMatchId?.Invoke(message.MatchId);
			_matchJoinedTcs.TrySetResult(true);
		}
		
		private void OnMatchEnded(MatchEndedMessage message) => MatchEndedWithMatchId?.Invoke(message.MatchId);

		private void OnDisconnectedWhileConnectingAndJoining()
		{
			_disconnectedTcs.TrySetResult(true);
		}

		private void OnDisconnectedByServer()
		{
			lock (_connectingLock)
				if (_connecting)
					return;
			TryDisconnectByServerIfNotConnected();
		}

		private void TryDisconnectByServerIfNotConnected()
		{
			lock (_connectedLock)
			{
				if (!_connected)
					return;
				if (_gameServerClient.IsConnected)
					return;
				DisconnectedByServer?.Invoke();
				_connected = false;
			}
		}
	}
}
