using System;
using System.Collections.Generic;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics
{
	[CreateAssetMenu(fileName = "ElympicsGameConfig", menuName = "Elympics/GameConfig")]
	public class ElympicsGameConfig : ScriptableObject
	{
		private const int OldInputsToSendBufferSizeTicksMin = 2;

		[SerializeField] private string elympicsEndpoint = "http://127.0.0.1:9101";
		[SerializeField] private string gameName         = "Game";
		[SerializeField] private string gameId           = "fe9b83a9-7d50-4299-859a-93fd313f420b";
		[SerializeField] private string gameVersion      = "1";
		[SerializeField] private int    players          = 2;
		[SerializeField] private string gameplayScene;
		[SerializeField] private Object gameplaySceneAsset;

		[SerializeField] private bool botsInServer = true;

		[SerializeField] private bool enableReconnect              = false;
		[SerializeField] private bool connectOnGameplaySceneLoad   = true;
		[SerializeField] private int  ticksPerSecond               = 30;
		[SerializeField] private int  snapshotSendingPeriodInTicks = 1;
		[SerializeField] private int  inputLagTicks                = 2;
		[SerializeField] private int  maxAllowedLagInTicks         = 15;
		[SerializeField] private bool prediction                   = true;
		[SerializeField] private int  predictionLimitInTicks       = 8;

		[SerializeField] private GameplaySceneDebugModeEnum mode = GameplaySceneDebugModeEnum.LocalPlayerAndBots;

		[SerializeField] private HalfRemoteModeEnum    halfRemoteMode            = HalfRemoteModeEnum.Server;
		[SerializeField] private string                ipForHalfRemoteMode       = "127.0.0.1";
		[SerializeField] private int                   portForHalfRemoteMode     = 9101;
		[SerializeField] private int                   playerIdForHalfRemoteMode = 1;
		[SerializeField] private List<InitialUserData> testPlayers;

		public string ElympicsEndpoint => elympicsEndpoint;
		public string GameName         => gameName;
		public string GameId           => gameId;
		public string GameVersion      => gameVersion;
		public int    Players          => players;
		public string GameplayScene    => gameplayScene;

		public bool BotsInServer => botsInServer;

		public bool  Prediction                   => prediction;
		public bool  ReconnectEnabled             => enableReconnect;
		public int   TicksPerSecond               => ticksPerSecond;
		public int   SnapshotSendingPeriodInTicks => snapshotSendingPeriodInTicks;
		public float TickDuration                 => 1.0f / ticksPerSecond;
		public int   InputLagTicks                => inputLagTicks;

		internal GameplaySceneDebugModeEnum GameplaySceneDebugMode      => mode;
		internal HalfRemoteModeEnum         HalfRemoteMode              => GetHalfRemoteMode(halfRemoteMode);
		public   string                     IpForHalfRemoteMode         => ipForHalfRemoteMode;
		public   int                        PortForHalfRemoteMode       => portForHalfRemoteMode;
		public   int                        InputsToSendBufferSize      => GetInputsToSendBufferSize();
		public   int                        PredictionBufferSize        => inputLagTicks + snapshotSendingPeriodInTicks + maxAllowedLagInTicks;
		public   int                        TotalPredictionLimitInTicks => inputLagTicks + snapshotSendingPeriodInTicks + predictionLimitInTicks;
		public   int                        PlayerIdForHalfRemoteMode   => GetHalfRemotePlayerId(playerIdForHalfRemoteMode);
		public   List<InitialUserData>      TestPlayers                 => testPlayers;

		[field: NonSerialized] public HalfRemoteLagConfig         HalfRemoteLagConfig     { get; }      = new HalfRemoteLagConfig();
		[field: NonSerialized] public ReconciliationFrequencyEnum ReconciliationFrequency { get; set; } = ReconciliationFrequencyEnum.OnlyIfNeeded;

		private int GetInputsToSendBufferSize() => inputLagTicks + Math.Max(maxAllowedLagInTicks, OldInputsToSendBufferSizeTicksMin);

		public static HalfRemoteModeEnum GetHalfRemoteMode(HalfRemoteModeEnum defaultHalfRemoteMode) => IsOverridenInHalfRemote()
			? ElympicsClonesManager.IsBot() ? HalfRemoteModeEnum.Bot : HalfRemoteModeEnum.Client
			: defaultHalfRemoteMode;

		public static bool IsOverridenInHalfRemote() => ElympicsClonesManager.IsClone();

		public static int GetHalfRemotePlayerId(int defaultPlayerId) => IsOverridenInHalfRemote()
			? ElympicsClonesManager.GetCloneNumber()
			: defaultPlayerId;

#if UNITY_EDITOR
		internal void UpdateGameVersion(string newGameVersion)
		{
			gameVersion = newGameVersion;
			EditorUtility.SetDirty(this);
		}
#endif
		public enum GameplaySceneDebugModeEnum
		{
			LocalPlayerAndBots,
			HalfRemote,
			DebugOnlinePlayer,
		}

		public enum HalfRemoteModeEnum
		{
			Server,
			Client,
			Bot
		}

		public enum ReconciliationFrequencyEnum
		{
			OnlyIfNeeded = 0,
			Never,
			OnEverySnapshot
		}

		[Serializable]
		public class InitialUserData
		{
			public string  userId;
			public bool    isBot;
			public double  botDifficulty;
			public byte[]  gameEngineData;
			public float[] matchmakerData;
		}
	}
}
