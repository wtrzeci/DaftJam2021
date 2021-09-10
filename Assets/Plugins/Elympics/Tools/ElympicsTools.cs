using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Elympics
{
	public class ElympicsTools
	{
		private const string TOOLS_MENU_PATH         = "Tools/Elympics/";
		public const  string RESET_IDS_MENU_PATH     = TOOLS_MENU_PATH + "Reset Network Ids";
		public const  string SETUP_MENU_PATH         = TOOLS_MENU_PATH + "Select or Create Config";
		private const string BUILD_WINDOWS_SERVER    = TOOLS_MENU_PATH + "Build Windows Server";
		private const string BUILD_LINUX_SERVER      = TOOLS_MENU_PATH + "Build Linux Server";
		private const string BUILD_AND_UPLOAD_SERVER = TOOLS_MENU_PATH + "Build and Upload Server";

#if UNITY_EDITOR
		[MenuItem(SETUP_MENU_PATH, priority = 1)]
		public static void SelectOrCreateConfig()
		{
			var config = ElympicsConfig.Load();
			if (config == null)
			{
				config = CreateNewConfig();
			}

			EditorUtility.FocusProjectWindow();
			Selection.activeObject = config;
		}

		[MenuItem(RESET_IDS_MENU_PATH, priority = 2)]
		public static void ResetIds()
		{
			NetworkIdEnumerator.Instance.Reset();

			var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(SceneManager.GetActiveScene(), true);

			ReassignNetworkIdsPreservingOrder(behaviours);
			AssignNetworkIdsForNewBehaviours(behaviours);
			CheckIfThereIsNoRepetitionsInNetworkIds(behaviours);
		}

		private static void ReassignNetworkIdsPreservingOrder(List<ElympicsBehaviour> behaviours)
		{
			var sortedBehaviours = new List<ElympicsBehaviour>();
			foreach (var behaviour in behaviours)
			{
				if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId && !behaviour.forceNetworkId)
					sortedBehaviours.Add(behaviour);
			}

			sortedBehaviours.Sort((x, y) => x.NetworkId.CompareTo(y.NetworkId));

			foreach (var behaviour in sortedBehaviours)
				AssignNextNetworkId(behaviour);
		}

		private static void AssignNextNetworkId(ElympicsBehaviour behaviour)
		{
			behaviour.UpdateSerializedNetworkId();
		}

		private static void AssignNetworkIdsForNewBehaviours(List<ElympicsBehaviour> behaviours)
		{
			foreach (var behaviour in behaviours)
			{
				if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId)
					continue;

				AssignNextNetworkId(behaviour);
			}
		}

		private static void CheckIfThereIsNoRepetitionsInNetworkIds(List<ElympicsBehaviour> behaviours)
		{
			var networkIds = new HashSet<int>();
			foreach (var behaviour in behaviours)
			{
				if (networkIds.Contains(behaviour.NetworkId))
				{
					Debug.LogError($"Repetition for network id {behaviour.NetworkId} in {behaviour.gameObject.name} {behaviour.GetType().Name}");
					continue;
				}

				networkIds.Add(behaviour.NetworkId);
			}
		}

		private static ElympicsConfig CreateNewConfig()
		{
			var newConfig = ScriptableObject.CreateInstance<ElympicsConfig>();
			// TODO: there is probably some hack possible to get path to current Elympics directory
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath("Assets/Plugins/Elympics/Resources/" + ElympicsConfig.PATH_IN_RESOURCES + ".asset");
			AssetDatabase.CreateAsset(newConfig, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return newConfig;
		}

		[MenuItem(BUILD_WINDOWS_SERVER, priority = 3)]
		private static void BuildServerWindows() => BuildTools.BuildServerWindows();

		[MenuItem(BUILD_LINUX_SERVER, priority = 4)]
		private static void BuildServerLinux() => BuildTools.BuildServerLinux();

		[MenuItem(BUILD_AND_UPLOAD_SERVER, priority = 5)]
		private static void BuildAndUploadServer() => BuildTools.BuildAndUploadServer();
#endif
	}
}