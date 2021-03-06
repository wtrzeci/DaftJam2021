using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsGameConfig))]
	internal partial class ElympicsGameConfigEditor : Editor
	{
		private const int MaxTicks         = 128;
		private const int MinTicks         = 1;
		private const int MaxLagInMs       = 2000;
		private const int MinInputLagTicks = 1;
		private const int MaxInputLagMs    = 500;
		private const int UnitLabelWidth   = 30;

		private EditorEndpointChecker _elympicsEndpointChecker;

		private ElympicsGameConfig _config;

		private SerializedProperty _elympicsEndpoint;
		private SerializedProperty _gameName;
		private SerializedProperty _gameId;
		private SerializedProperty _gameVersion;
		private SerializedProperty _players;
		private SerializedProperty _gameplaySceneAsset;
		private SerializedProperty _gameplayScene;

		private SerializedProperty _botsInServer;

		private SerializedProperty _enableReconnect;
		private SerializedProperty _ticksPerSecond;
		private SerializedProperty _snapshotSendingPeriodInTicks;
		private SerializedProperty _inputLagTicks;
		private SerializedProperty _maxAllowedLagInTicks;
		private SerializedProperty _prediction;
		private SerializedProperty _predictionLimitInTicks;

		private SerializedProperty _mode;
		private SerializedProperty _halfRemoteMode;
		private SerializedProperty _ipForHalfRemoteMode;
		private SerializedProperty _portForHalfRemoteMode;
		private SerializedProperty _playerIdForHalfRemoteMode;
		private SerializedProperty _testPlayers;

		private GUIStyle     _indentation;
		private Stack<float> _labelWidthStack;

		private void OnEnable()
		{
			_config = serializedObject.targetObject as ElympicsGameConfig;

			_elympicsEndpoint = serializedObject.FindProperty("elympicsEndpoint");
			_gameName = serializedObject.FindProperty("gameName");
			_gameId = serializedObject.FindProperty("gameId");
			_gameVersion = serializedObject.FindProperty("gameVersion");
			_players = serializedObject.FindProperty("players");
			_gameplaySceneAsset = serializedObject.FindProperty("gameplaySceneAsset");
			_gameplayScene = serializedObject.FindProperty("gameplayScene");

			_botsInServer = serializedObject.FindProperty("botsInServer");

			_enableReconnect = serializedObject.FindProperty("enableReconnect");
			_ticksPerSecond = serializedObject.FindProperty("ticksPerSecond");
			_snapshotSendingPeriodInTicks = serializedObject.FindProperty("snapshotSendingPeriodInTicks");
			_inputLagTicks = serializedObject.FindProperty("inputLagTicks");
			_maxAllowedLagInTicks = serializedObject.FindProperty("maxAllowedLagInTicks");
			_prediction = serializedObject.FindProperty("prediction");
			_predictionLimitInTicks = serializedObject.FindProperty("predictionLimitInTicks");

			_mode = serializedObject.FindProperty("mode");
			_halfRemoteMode = serializedObject.FindProperty("halfRemoteMode");
			_ipForHalfRemoteMode = serializedObject.FindProperty("ipForHalfRemoteMode");
			_portForHalfRemoteMode = serializedObject.FindProperty("portForHalfRemoteMode");
			_playerIdForHalfRemoteMode = serializedObject.FindProperty("playerIdForHalfRemoteMode");
			_testPlayers = serializedObject.FindProperty("testPlayers");

			_indentation = new GUIStyle {margin = new RectOffset(10, 0, 0, 0)};
			_labelWidthStack = new Stack<float>();

			_elympicsEndpointChecker = new EditorEndpointChecker();
			_elympicsEndpointChecker.UpdateUri(_elympicsEndpoint.stringValue);

			EditorApplication.update += EditorUpdate;
			Undo.undoRedoPerformed += UpdateUriOnUndoRedo;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UpdateUriOnUndoRedo;
		}

		private void EditorUpdate()
		{
			_elympicsEndpointChecker.Update();
		}

		private void UpdateUriOnUndoRedo()
		{
			serializedObject.Update();
			_elympicsEndpointChecker.UpdateUri(_elympicsEndpoint.stringValue);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var summaryLabelStyle = new GUIStyle(GUI.skin.label) {fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true};

			var cachedWordWrap = EditorStyles.label.wordWrap;
			EditorStyles.label.wordWrap = true;

			EditorEndpointCheckerEditor.DrawEndpointField(serializedObject, _elympicsEndpointChecker, _elympicsEndpoint, "Elympics endpoint");

			BeginSection("Game");
			EditorGUILayout.PropertyField(_gameName, new GUIContent("Game name"));
			EditorGUILayout.PropertyField(_gameId, new GUIContent("Game id"));
			EditorGUILayout.PropertyField(_gameVersion, new GUIContent("Game version"));
			EditorGUILayout.PropertyField(_players, new GUIContent("Players number"));
			serializedObject.ApplyModifiedProperties();
			DrawGameplayScene();
			EndSection();
			EditorGUILayout.Separator();

			BeginSection("Server");
			EditorGUILayout.PropertyField(_botsInServer, new GUIContent("Bots inside server"));
			EditorGUILayout.LabelField(Label_BotsInsideServerSummary, summaryLabelStyle);
			serializedObject.ApplyModifiedProperties();
			EndSection();

			BeginSection("Client");
			EditorGUILayout.PropertyField(_enableReconnect, new GUIContent("Reconnect to match"));
			_ticksPerSecond.intValue = TickSlider("Ticks per second", _ticksPerSecond.intValue, MinTicks, MaxTicks);
			_snapshotSendingPeriodInTicks.intValue = TickSlider("Send snapshot every", _snapshotSendingPeriodInTicks.intValue,
				MinTicks, _ticksPerSecond.intValue);
			_inputLagTicks.intValue = TickSliderConvertedToMs("Input lag", _inputLagTicks.intValue, MinInputLagTicks, MsToTicks(MaxInputLagMs));
			_maxAllowedLagInTicks.intValue = Math.Max(
				TickSliderConvertedToMs("Max allowed lag", _maxAllowedLagInTicks.intValue, 0, MsToTicks(MaxLagInMs)),
				0
			);
			EditorGUILayout.PropertyField(_prediction, new GUIContent("Prediction"));
			_predictionLimitInTicks.intValue = TickSliderConvertedToMs("Prediction limit", _predictionLimitInTicks.intValue,
				0, _maxAllowedLagInTicks.intValue);
			serializedObject.ApplyModifiedProperties();
			EditorGUILayout.LabelField(new GUIContent("Total prediction limit", "With input lag and snapshot sending period included"),
				new GUIContent($"{TicksToMs(_config.TotalPredictionLimitInTicks)} ms"), new GUIStyle(EditorStyles.label) {alignment = TextAnchor.MiddleRight});
			EndSection();
			EditorGUILayout.Separator();

			BeginSection("Development");
			EditorGUILayout.PropertyField(_mode, new GUIContent("Mode"));
			switch ((ElympicsGameConfig.GameplaySceneDebugModeEnum) _mode.enumValueIndex)
			{
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots:
					break;
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer:
					break;
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote:
					DrawHalfRemote();
					break;
			}

			DrawInitialUserDatas();
			serializedObject.ApplyModifiedProperties();
			EndSection();

			BeginSection("Manage games in Elympics");
			EditorGUILayout.LabelField(Label_CreateGameSummary, summaryLabelStyle);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Create", GUILayout.Width(100), GUILayout.MaxHeight(20)))
			{
				if (!IsConnectedToElympics())
					return;

				ElympicsWebIntegration.CreateGame(_gameName, _gameId);
				GUIUtility.ExitGUI();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.LabelField(Label_UploadSummary, summaryLabelStyle);
			if (GUILayout.Button("Upload", GUILayout.Width(100), GUILayout.MaxHeight(20)))
			{
				if (!IsConnectedToElympics())
					return;

				ElympicsWebIntegration.UploadGame();
				GUIUtility.ExitGUI();
			}

			serializedObject.ApplyModifiedProperties();
			EndSection();

			EditorStyles.label.wordWrap = cachedWordWrap;
		}


		private bool IsConnectedToElympics()
		{
			if (!EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
			{
				Debug.LogError("Cannot connect with ElympicsWeb, check ElympicsWeb endpoint");
				return false;
			}

			return true;
		}

		private void BeginSection(string header)
		{
			EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(_indentation);
			_labelWidthStack.Push(EditorGUIUtility.labelWidth);
			EditorGUIUtility.labelWidth -= _indentation.margin.left;
		}

		private void EndSection()
		{
			EditorGUIUtility.labelWidth = _labelWidthStack.Pop();
			EditorGUILayout.EndVertical();
		}

		private void DrawGameplayScene()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(_gameplaySceneAsset, new GUIContent("Gameplay scene"));
			if (EditorGUI.EndChangeCheck())
				CheckGameplaySceneAndUpdatePath();

			if (GUILayout.Button("Open scene"))
				EditorSceneManager.OpenScene(_gameplayScene.stringValue);
			EditorGUILayout.EndHorizontal();
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(_gameplayScene, new GUIContent("Gameplay scene path"));
			EditorGUI.EndDisabledGroup();
		}

		private void CheckGameplaySceneAndUpdatePath()
		{
			var previousValue = _gameplaySceneAsset.GetValue();
			serializedObject.ApplyModifiedProperties();

			var newValue = _gameplaySceneAsset.GetValue();
			if (!(newValue is SceneAsset scene))
			{
				_gameplaySceneAsset.SetValue(previousValue);
				serializedObject.ApplyModifiedProperties();
				return;
			}

			var scenePath = AssetDatabase.GetAssetOrScenePath(scene);
			_gameplayScene.stringValue = scenePath;
		}

		private void DrawHalfRemote()
		{
			if (ElympicsGameConfig.IsOverridenInHalfRemote())
				EditorGUI.BeginDisabledGroup(true);

			const string remoteModeDescription = "Half remote mode";

			ElympicsGameConfig.HalfRemoteModeEnum halfRemoteMode;
			if (ElympicsGameConfig.IsOverridenInHalfRemote())
			{
				halfRemoteMode = (ElympicsGameConfig.HalfRemoteModeEnum) _halfRemoteMode.enumValueIndex;
				halfRemoteMode = ElympicsGameConfig.GetHalfRemoteMode(halfRemoteMode);
				EditorGUILayout.EnumPopup(remoteModeDescription, halfRemoteMode);
			}
			else
			{
				EditorGUILayout.PropertyField(_halfRemoteMode, new GUIContent(remoteModeDescription));
				halfRemoteMode = (ElympicsGameConfig.HalfRemoteModeEnum) _halfRemoteMode.enumValueIndex;
			}

			switch (halfRemoteMode)
			{
				case ElympicsGameConfig.HalfRemoteModeEnum.Server:
					EditorGUILayout.PropertyField(_portForHalfRemoteMode, new GUIContent("Port server listens on"));
					break;
				case ElympicsGameConfig.HalfRemoteModeEnum.Client:
				case ElympicsGameConfig.HalfRemoteModeEnum.Bot:
					EditorGUILayout.PropertyField(_ipForHalfRemoteMode, new GUIContent("IP Address of server"));
					EditorGUILayout.PropertyField(_portForHalfRemoteMode, new GUIContent("Port of server"));
					if (ElympicsGameConfig.IsOverridenInHalfRemote())
						EditorGUILayout.IntField("Used player id", ElympicsGameConfig.GetHalfRemotePlayerId(_playerIdForHalfRemoteMode.intValue));
					else
						EditorGUILayout.PropertyField(_playerIdForHalfRemoteMode, new GUIContent("Used player id"));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (ElympicsGameConfig.IsOverridenInHalfRemote())
				EditorGUI.EndDisabledGroup();

			if (halfRemoteMode == ElympicsGameConfig.HalfRemoteModeEnum.Client)
			{
				EditorGUILayout.LabelField("Lag");
				EditorGUILayout.BeginVertical(_indentation);
				_config.HalfRemoteLagConfig.DelayMs = EditorGUILayout.IntField("Delay milliseconds", _config.HalfRemoteLagConfig.DelayMs);
				_config.HalfRemoteLagConfig.PacketLoss = EditorGUILayout.FloatField("Packet loss", _config.HalfRemoteLagConfig.PacketLoss);
				_config.HalfRemoteLagConfig.JitterMs = EditorGUILayout.IntField("Jitter milliseconds (gaussian)", _config.HalfRemoteLagConfig.JitterMs);
				_config.HalfRemoteLagConfig.RandomSeed = EditorGUILayout.IntField("Random seed", _config.HalfRemoteLagConfig.RandomSeed);
				EditorGUILayout.LabelField("Preset");
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("LAN")) _config.HalfRemoteLagConfig.LoadLan();
				if (GUILayout.Button("Broadband")) _config.HalfRemoteLagConfig.LoadBroadband();
				if (GUILayout.Button("Slow broadband")) _config.HalfRemoteLagConfig.LoadSlowBroadband();
				if (GUILayout.Button("LTE")) _config.HalfRemoteLagConfig.LoadLTE();
				if (GUILayout.Button("3G")) _config.HalfRemoteLagConfig.Load3G();
				if (GUILayout.Button("Total mess")) _config.HalfRemoteLagConfig.LoadTotalMess();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				EditorGUILayout.Separator();

				_config.ReconciliationFrequency =
					(ElympicsGameConfig.ReconciliationFrequencyEnum) EditorGUILayout.EnumPopup("Reconcile", _config.ReconciliationFrequency);
			}

			EditorGUILayout.Separator();
		}

		private void DrawInitialUserDatas()
		{
			EditorGUILayout.LabelField("Test players", new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Italic});
			if (!_testPlayers.isArray)
				_testPlayers.SetValue(new List<ElympicsGameConfig.InitialUserData>());

			var wantedArraySize = Mathf.Clamp(_players.intValue, 1, 32);
			while (_testPlayers.arraySize < wantedArraySize)
				_testPlayers.InsertArrayElementAtIndex(_testPlayers.arraySize);

			while (_testPlayers.arraySize > wantedArraySize)
				_testPlayers.DeleteArrayElementAtIndex(_testPlayers.arraySize - 1);

			for (var i = 0; i < _testPlayers.arraySize; i++)
			{
				EditorGUILayout.PropertyField(_testPlayers.GetArrayElementAtIndex(i));
				EditorGUILayout.Separator();
			}
		}

		private int TickSliderConvertedToMs(string label, int value, int left, int right) => TickSliderConvertedToMs(new GUIContent(label), value, left, right);

		private int TickSliderConvertedToMs(GUIContent content, int value, int left, int right)
		{
			var newMsValue = IntSliderWithUnit(content, TicksToMs(value), TicksToMs(left), TicksToMs(right), "ms");
			return Mathf.Clamp(MsToTicks(newMsValue), left, right);
		}

		private int TickSlider(string label, int value, int left, int right) => TickSlider(new GUIContent(label), value, left, right);

		private int TickSlider(GUIContent content, int value, int left, int right)
		{
			return IntSliderWithUnit(content, value, left, right, "ticks");
		}

		private int IntSliderWithUnit(GUIContent content, int value, int left, int right, string unit)
		{
			EditorGUILayout.BeginHorizontal();
			var newValue = EditorGUILayout.IntSlider(content, value, left, right, GUILayout.MaxWidth(float.MaxValue));
			EditorGUILayout.LabelField(unit, GUILayout.Width(UnitLabelWidth));
			EditorGUILayout.EndHorizontal();
			return newValue;
		}

		private int TicksToMs(int ticks)
		{
			return (int) Math.Round(ticks * 1000.0 / _ticksPerSecond.intValue);
		}

		private int MsToTicks(int milliseconds)
		{
			return (int) Math.Round(_ticksPerSecond.intValue * milliseconds / 1000.0);
		}

		[CustomPropertyDrawer(typeof(ElympicsGameConfig.InitialUserData))]
		public class InitialUserDataPropertyDrawer : PropertyDrawer
		{
			private static readonly string[] GameEngineDataShowTypes = {"Base64", "String"};

			private readonly Dictionary<int, bool> _showPlayers            = new Dictionary<int, bool>();
			private readonly Dictionary<int, int>  _gameEngineDataShowType = new Dictionary<int, int>();

			private static readonly Regex NumberRegex = new Regex(@"\d+");

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				var index = int.Parse(NumberRegex.Match(property.propertyPath).Value, NumberFormatInfo.InvariantInfo);
				if (!_showPlayers.TryGetValue(index, out var showPlayer))
					_showPlayers.Add(index, default);

				showPlayer = EditorGUI.Foldout(position, showPlayer, $"Player {index}", new GUIStyle(EditorStyles.foldout) {margin = new RectOffset(10, 0, 0, 0)});
				_showPlayers[index] = showPlayer;
				if (!showPlayer)
					return;

				EditorGUI.indentLevel += 1;
				var userId = property.FindPropertyRelative("userId");
				var isBot = property.FindPropertyRelative("isBot");
				var botDifficulty = property.FindPropertyRelative("botDifficulty");
				var gameEngineData = property.FindPropertyRelative("gameEngineData");
				var matchmakerData = property.FindPropertyRelative("matchmakerData");
				EditorGUILayout.PropertyField(userId);
				if (string.IsNullOrEmpty(userId.stringValue))
					userId.stringValue = index.ToString();

				EditorGUILayout.PropertyField(isBot);
				if (isBot.boolValue)
					EditorGUILayout.PropertyField(botDifficulty);

				DrawGameEngineData(gameEngineData, index);
				DrawMatchmakerData(matchmakerData);

				EditorGUI.indentLevel -= 1;
			}

			private void DrawGameEngineData(SerializedProperty gameEngineData, int index)
			{
				if (!_gameEngineDataShowType.TryGetValue(index, out var gameEngineDataShowTypeChosen))
					_gameEngineDataShowType.Add(index, default);

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Game engine data");
				EditorGUI.BeginChangeCheck();
				gameEngineDataShowTypeChosen = GUILayout.Toolbar(gameEngineDataShowTypeChosen, GameEngineDataShowTypes, GUI.skin.button, GUI.ToolbarButtonSize.FitToContents);
				if (EditorGUI.EndChangeCheck())
				{
					_gameEngineDataShowType[index] = gameEngineDataShowTypeChosen;
					GUI.FocusControl(null);
				}

				EditorGUILayout.EndHorizontal();

				try
				{
					var gameEngineDataValue = (byte[]) gameEngineData.GetValue();
					byte[] newGameEngineDataValue = null;
					switch (GameEngineDataShowTypes[gameEngineDataShowTypeChosen])
					{
						case "Base64":
						{
							var gameEngineDataAsByte64 = Convert.ToBase64String(gameEngineDataValue);
							var newGameEngineDataAsByte64 = EditorGUILayout.TextArea(gameEngineDataAsByte64, new GUIStyle(GUI.skin.textArea) {fixedHeight = 0, stretchWidth = true, stretchHeight = true});
							newGameEngineDataValue = newGameEngineDataAsByte64.Length != 0
								? Convert.FromBase64String(newGameEngineDataAsByte64)
								: new byte[0];
							break;
						}
						case "String":
						{
							var gameEngineDataAsString = Encoding.ASCII.GetString(gameEngineDataValue);
							var newGameEngineDataAsString = EditorGUILayout.TextArea(gameEngineDataAsString, new GUIStyle(GUI.skin.textArea) {fixedHeight = 0, stretchWidth = true, stretchHeight = true});
							newGameEngineDataValue = newGameEngineDataAsString.Length != 0
								? Encoding.ASCII.GetBytes(newGameEngineDataAsString)
								: new byte[0];
							break;
						}
					}

					gameEngineData.SetValue(newGameEngineDataValue);
				}
				catch (FormatException e)
				{
					Debug.LogWarning(e.Message);
				}
			}

			private static void DrawMatchmakerData(SerializedProperty matchmakerData)
			{
				var mmDataValue = (float[]) matchmakerData.GetValue();
				EditorGUI.BeginChangeCheck();
				var newMmDataValueString = EditorGUILayout.TextField("Matchmaker data", string.Join(" ", mmDataValue.Select(x => x.ToString(CultureInfo.InvariantCulture))));
				if (EditorGUI.EndChangeCheck())
				{
					var newMmDataValue = newMmDataValueString.Length != 0
						? newMmDataValueString.Split(' ').Select(x => float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : 0f).ToArray()
						: new float[0];
					matchmakerData.SetValue(newMmDataValue);
				}
			}
		}
	}
}
