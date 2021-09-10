using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using UnityConnectors;
using Random = System.Random;

namespace Elympics
{
	public class HalfRemoteMatchClientAdapter : IMatchClient
	{
		private const int MinSynchronizationDelay = 200;
		private const int MaxJitterMultiplier     = 3;

		public event Action<TimeSynchronizationData> Synchronized;
		public event Action<ElympicsSnapshot>        SnapshotReceived;
		public event Action<byte[]>                  RawSnapshotReceived;

		private readonly RingBuffer<byte[]> _inputRingBuffer;

		private readonly HalfRemoteLagConfig _lagConfig;
		private readonly Random              _lagRandom;

		private string                _userId;
		private TcpClient             _tcpClient;
		private HalfRemoteMatchClient _client;
		private bool                  _playerDisconnected;

		public HalfRemoteMatchClientAdapter(ElympicsGameConfig config)
		{
			_inputRingBuffer = new RingBuffer<byte[]>(config.InputsToSendBufferSize);
			_lagConfig = config.HalfRemoteLagConfig;
			_lagRandom = new Random(config.HalfRemoteLagConfig.RandomSeed);
		}

		internal void ConnectToServer(TcpClient tcpClient, string userId)
		{
			_userId = userId;
			_tcpClient = tcpClient;
			_client = new HalfRemoteMatchClient(tcpClient, userId);
			_client.InGameDataForPlayerOnReliableChannelGenerated += OnInGameDataForPlayerOnReliableChannelGenerated;
			_client.InGameDataForPlayerOnUnreliableChannelGenerated += OnInGameDataForPlayerOnUnreliableChannelGenerated;

			Task.Factory.StartNew(Synchronization, TaskCreationOptions.LongRunning);
		}

		public void PlayerConnected() => _client.PlayerConnected();

		public void PlayerDisconnected()
		{
			_client.PlayerDisconnected();
			_playerDisconnected = true;
		}

		public async Task SendInputReliable(ElympicsInput input)   => SendRawInputReliable(input.Serialize());
		public async Task SendInputUnreliable(ElympicsInput input) => SendRawInputUnreliable(input.Serialize());

		public void OnInGameDataForPlayerOnReliableChannelGenerated(byte[] data, string userId)
		{
			if (userId != _userId)
				return;
			_ = RunWithLag(() =>
			{
				SnapshotReceived?.Invoke(ElympicsSnapshotSerializer.Deserialize(data));
				RawSnapshotReceived?.Invoke(data);
			});
		}

		public void OnInGameDataForPlayerOnUnreliableChannelGenerated(byte[] data, string userId)
		{
			if (userId != _userId)
				return;
			_ = RunWithLag(() =>
			{
				SnapshotReceived?.Invoke(ElympicsSnapshotSerializer.Deserialize(data));
				RawSnapshotReceived?.Invoke(data);
			});
		}

		public void SendRawInputReliable(byte[] data) => _ = RunWithLag(() => _client.SendInputReliable(data));

		public void SendRawInputUnreliable(byte[] data)
		{
			_inputRingBuffer.PushBack(data);
			var serializedInputs = ElympicsInputSerializer.MergeInputsToPackage(_inputRingBuffer.ToArray());
			_ = RunWithLag(() => _client.SendInputUnreliable(serializedInputs));
		}

		private async Task RunWithLag(Action action)
		{
			GetNewLag(out var lost, out var lagMs);
			if (lost)
				return;
			if (lagMs != 0)
				await Task.Delay(lagMs);

			action.Invoke();
		}

		private void GetNewLag(out bool lost, out int lagMs)
		{
			lost = _lagRandom.NextDouble() < _lagConfig.PacketLoss;
			lagMs = (int) NextGaussian(_lagConfig.DelayMs + _lagConfig.JitterMs, _lagConfig.JitterMs);
			lagMs = Math.Max(lagMs, _lagConfig.DelayMs);
			lagMs = Math.Min(lagMs, _lagConfig.DelayMs + MaxJitterMultiplier * _lagConfig.JitterMs);
		}

		private double NextGaussian(double mean = 0.0, double stdDev = 1.0)
		{
			var u1 = 1.0 - _lagRandom.NextDouble();
			var u2 = 1.0 - _lagRandom.NextDouble();
			var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
			return mean + stdDev * randStdNormal;
		}

		private async Task Synchronization()
		{
			while (NotDisconnected())
			{
				GetNewLag(out var lost, out var lagMs);
				if (!lost)
				{
					var rtt = TimeSpan.FromMilliseconds(2 * lagMs);
					var timeSynchronizationData = new TimeSynchronizationData
					{
						RoundTripDelay = rtt,
						LocalClockOffset = TimeSpan.Zero,
						UnreliableWaitingForFirstPing = false,
						UnreliableReceivedAnyPing = true,
						UnreliableReceivedPingLately = true,
						UnreliableLocalClockOffset = TimeSpan.Zero,
						UnreliableLastReceivedPingDateTime = DateTime.Now,
						UnreliableRoundTripDelay = rtt,
					};
					Synchronized?.Invoke(timeSynchronizationData);
				}

				var delay = Math.Max(lagMs, MinSynchronizationDelay);
				await Task.Delay(delay);
			}
		}

		private bool NotDisconnected() => !_playerDisconnected && _tcpClient != null && _tcpClient.Connected;
	}
}
