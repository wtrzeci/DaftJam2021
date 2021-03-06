#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elympics
{
	public static class BuildTools
	{
		private const string ServerBuildPath    = "serverbuild";
		private const string EngineSubdirectory = "Engine";
		private const string BotSubdirectory    = "Bot";
		private const string UnityBuildPath     = "Unity";

		private const string EngineWrapperFilename = "GameEngine.dll";
		private const string BotWrapperFilename    = "GameBot.dll";

		private const string ServerBuildAppNameLinux   = "Unity";
		private const string ServerBuildAppNameWindows = "Unity.exe";

		private static readonly string ServerWrapperPath            = Path.Combine("Assets", "Plugins", "Elympics", "GameEngine", "Wrapper");
		private static readonly string GameBotNoopPath              = Path.Combine("Assets", "Plugins", "Elympics", "GameEngine", "GameBotNoop");
		private const           string ServerWrapperFilesPattern    = "*.dll_";
		private const           string ServerWrapperTargetExtension = ".dll";

		internal static string EnginePath => Path.Combine(ServerBuildPath, EngineSubdirectory);
		internal static string BotPath    => Path.Combine(ServerBuildPath, BotSubdirectory);

		public static void UpdateElympicsGameVersion(string newGameVersion)
		{
			var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
			if (config == null)
				throw new Exception("[Elympics] Elympics config not found");

			config.UpdateGameVersion(newGameVersion);
		}

		internal static void BuildServerWindows()
		{
			BuildServer(ServerBuildAppNameWindows, BuildTarget.StandaloneWindows64);
		}

		internal static void BuildServerLinux()
		{
			BuildServer(ServerBuildAppNameLinux, BuildTarget.StandaloneLinux64);
		}

		internal static void BuildAndUploadServer()
		{
			BuildServerLinux();
			AmazonS3Tools.UploadToS3Async();
		}

		private static void BuildServer(string appName, BuildTarget target)
		{
			try
			{
				var title = $"Building server for {appName}";
				EditorUtility.DisplayProgressBar(title, "Loading elympics game config", 0);
				var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
				if (config == null)
					throw new Exception("[Elympics] Elympics config not found");

				var sceneToBuild = new[] {config.GameplayScene};
				EditorUtility.DisplayProgressBar(title, $"Using scene {config.GameplayScene}", 0.15f);

				var buildTargetGroup = BuildTargetGroup.Standalone;
				var oldScriptingBackend = PlayerSettings.GetScriptingBackend(buildTargetGroup);
				PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.Mono2x);

				EditorUtility.DisplayProgressBar(title, "Removing old server build path", 0.3f);
				if (Directory.Exists(ServerBuildPath))
					Directory.Delete(ServerBuildPath, true);

				EditorUtility.DisplayProgressBar(title, "Building player", 0.45f);
				var buildPlayerOptions = new BuildPlayerOptions
				{
					scenes = sceneToBuild,
					locationPathName = Path.Combine(ServerBuildPath, EngineSubdirectory, UnityBuildPath, appName),
					targetGroup = buildTargetGroup,
					target = target,
					options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4HC
				};

				var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
				LogReport(report);

				// Restore
				PlayerSettings.SetScriptingBackend(buildTargetGroup, oldScriptingBackend);

				if (report.summary.result != BuildResult.Succeeded)
					return;


				EditorUtility.DisplayProgressBar(title, "Copying engine wrapper to build path", 0.6f);
				CopyWrapperToBuildPath(BotWrapperFilename, EngineSubdirectory);

				EditorUtility.DisplayProgressBar(title, "Copying bot wrapper to build path", 0.75f);
				if (config.BotsInServer)
					CopyGameBotNoopToBuildPath();
				else
					CopyWrapperToBuildPath(EngineWrapperFilename, BotSubdirectory);

				EditorUtility.DisplayProgressBar(title, $"Build finished at {ServerBuildPath}", 1f);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static void CopyWrapperToBuildPath(string excludedFilename, string subdirectory)
		{
			Directory.CreateDirectory(Path.Combine(ServerBuildPath, subdirectory));
			var wrapperFiles = Directory.GetFiles(ServerWrapperPath, ServerWrapperFilesPattern);
			wrapperFiles = wrapperFiles.Where(x => !x.Contains(excludedFilename)).ToArray();
			foreach (var wrapperFile in wrapperFiles)
			{
				var filename = Path.GetFileName(wrapperFile);
				var targetFile = Path.Combine(ServerBuildPath, subdirectory, filename);
				targetFile = Path.ChangeExtension(targetFile, ServerWrapperTargetExtension);
				File.Copy(wrapperFile, targetFile);
			}
		}

		private static void CopyGameBotNoopToBuildPath()
		{
			Directory.CreateDirectory(Path.Combine(ServerBuildPath, BotSubdirectory));
			var botFiles = Directory.GetFiles(GameBotNoopPath, ServerWrapperFilesPattern);
			foreach (var botFile in botFiles)
			{
				var filename = Path.GetFileName(botFile);
				var targetFile = Path.Combine(ServerBuildPath, BotSubdirectory, filename);
				targetFile = Path.ChangeExtension(targetFile, ServerWrapperTargetExtension);
				File.Copy(botFile, targetFile);
			}
		}

		private static void LogReport(BuildReport report)
		{
			if (report.summary.result == BuildResult.Succeeded)
				Debug.Log($"Server build succeeded on {report.summary.outputPath}");
			else
				Debug.LogError($"Server build failed with {report.summary.totalErrors} errors");
		}
	}
}
#endif