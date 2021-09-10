using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	public class HalfRemoteMatchConnectClient : IMatchConnectClient
	{
		private static readonly TimeSpan WaitTimeToRetryConnect = TimeSpan.FromSeconds(1);

		public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
		public event Action                          ConnectingFailed;
		public event Action<string>                  AuthenticatedUserMatchWithUserId;
		public event Action<string>                  AuthenticatedUserMatchFailedWithError;
		public event Action<string>                  MatchJoinedWithError;
		public event Action<string>                  MatchJoinedWithMatchId;
		public event Action<string>                  MatchEndedWithMatchId;
		public event Action                          DisconnectedByServer;
		public event Action                          DisconnectedByClient;

		private static readonly string MatchId = Guid.NewGuid().ToString();

		private readonly HalfRemoteMatchClientAdapter _halfRemoteMatchClientAdapter;
		private readonly string                       _ip;
		private readonly int                          _port;
		private readonly string                       _userId;
		private          TcpClient                    _tcpClient;

		public HalfRemoteMatchConnectClient(HalfRemoteMatchClientAdapter halfRemoteMatchClientAdapter, string ip, int port, string userId)
		{
			_halfRemoteMatchClientAdapter = halfRemoteMatchClientAdapter;
			_ip = ip;
			_port = port;
			_userId = userId;
		}

		public async Task<bool> ConnectAndJoinAsPlayer()
		{
			while (true)
			{
				try
				{
					_tcpClient = new TcpClient();
					await _tcpClient.ConnectAsync(IPAddress.Parse(_ip), _port);
					break;
				}
				catch (Exception e)
				{
					_tcpClient = null;
					await Task.Delay(WaitTimeToRetryConnect);
					Debug.LogException(e);
				}
			}

			if (_tcpClient == null)
				return false;

			_halfRemoteMatchClientAdapter.ConnectToServer(_tcpClient, _userId);
			_halfRemoteMatchClientAdapter.PlayerConnected();
			ConnectedWithSynchronizationData?.Invoke(new TimeSynchronizationData {LocalClockOffset = TimeSpan.Zero, RoundTripDelay = TimeSpan.Zero, UnreliableReceivedAnyPing = false, UnreliableWaitingForFirstPing = true});
			AuthenticatedUserMatchWithUserId?.Invoke(_userId);
			MatchJoinedWithMatchId?.Invoke(MatchId);
			return true;
		}

		public Task<bool> ConnectAndJoinAsSpectator() => Task.FromResult(false);

		public void Disconnect()
		{
			_halfRemoteMatchClientAdapter.PlayerDisconnected();
			_tcpClient?.Close();
			DisconnectedByServer?.Invoke();
		}
	}
}
