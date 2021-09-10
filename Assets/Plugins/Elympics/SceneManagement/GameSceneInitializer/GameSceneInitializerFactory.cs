using System;

namespace Elympics
{
	internal static class GameSceneInitializerFactory
	{
		private const string ElympicsEnvironmentVariable    = "ELYMPICS";
		private const string ElympicsBotEnvironmentVariable = "ELYMPICS_BOT";

		#region MultiSceneData

		private static HalfRemoteGameServerInitializer _halfRemoteGameServerInitializer;
		private static int                             _playersInitialized;

		#endregion

		public static GameSceneInitializer Create(ElympicsGameConfig elympicsGameConfig)
		{
			#region Build run

			if (ShouldLoadElympicsOnlineBot())
				return new OnlineGameBotInitializer();
			if (ShouldLoadElympicsOnlineServer())
				return new OnlineGameServerInitializer();
			if (ShouldLoadElympicsOnlineClient())
				return new OnlineGameClientInitializer();

			if (ShouldLoadHalfRemoteServer())
				return new HalfRemoteGameServerInitializer();
			if (ShouldLoadHalfRemoteClient())
				return new HalfRemoteGameClientInitializer();

			#endregion

			switch (elympicsGameConfig.GameplaySceneDebugMode)
			{
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots:
					return InitializeLocalPlayerAndBots(elympicsGameConfig);
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote:
					return InitializeHalfRemotePlayers(elympicsGameConfig);
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer:
					return InitializeDebugOnlinePlayer();
				default:
					throw new ArgumentOutOfRangeException(nameof(elympicsGameConfig.GameplaySceneDebugMode));
			}
		}

		private static bool ShouldLoadElympicsOnlineServer() => IsElympicsEnvironmentVariableDefined() && !IsElympicsBotEnvironmentVariableDefined();
		private static bool ShouldLoadElympicsOnlineBot()    => IsElympicsEnvironmentVariableDefined() && IsElympicsBotEnvironmentVariableDefined();
		private static bool ShouldLoadElympicsOnlineClient() => ElympicsLobbyClient.Instance != null;

		private static bool IsElympicsBotEnvironmentVariableDefined() => Environment.GetEnvironmentVariables().Contains(ElympicsBotEnvironmentVariable);
		private static bool IsElympicsEnvironmentVariableDefined()    => Environment.GetEnvironmentVariables().Contains(ElympicsEnvironmentVariable);

		private static bool ShouldLoadHalfRemoteServer() => IsUnityServer();
		private static bool ShouldLoadHalfRemoteClient() => IsUnityStandalone() && !IsUnityEditor();

		private static bool IsUnityServer()
		{
#if UNITY_SERVER
			return true;
#else
			return false;
#endif
		}

		private static bool IsUnityStandalone()
		{
#if UNITY_STANDALONE
			return true;
#else
			return false;
#endif
		}

		private static bool IsUnityEditor()
		{
#if UNITY_EDITOR
			return true;
#else
			return false;
#endif
		}

		private static GameSceneInitializer InitializeLocalPlayerAndBots(ElympicsGameConfig elympicsGameConfig)
		{
			return new LocalGameServerInitializer();
		}

		private static DebugOnlineClientInitializer InitializeDebugOnlinePlayer() => new DebugOnlineClientInitializer();

		private static GameSceneInitializer InitializeHalfRemotePlayers(ElympicsGameConfig elympicsGameConfig)
		{
			switch (elympicsGameConfig.HalfRemoteMode)
			{
				case ElympicsGameConfig.HalfRemoteModeEnum.Server:
					return new HalfRemoteGameServerInitializer();
				case ElympicsGameConfig.HalfRemoteModeEnum.Client:
					return new HalfRemoteGameClientInitializer();
				case ElympicsGameConfig.HalfRemoteModeEnum.Bot:
					return new HalfRemoteGameBotInitializer();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
