using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsConfig))]
	public class ElympicsConfigEditor : Editor
	{
		private SerializedProperty _currentGameIndex;
		private SerializedProperty _availableGames;
		private SerializedProperty _game;

		private Object                _lastChosenGamePropertyObject;
		private Editor                _lastChosenGameEditor;
		private EditorEndpointChecker _elympicsWebEndpointChecker;
		private SerializedProperty    _elympicsWebEndpoint;

		private GUIStyle     _indentation;
		private Stack<float> _labelWidthStack;

		private void OnEnable()
		{
			_elympicsWebEndpoint = serializedObject.FindProperty("elympicsWebEndpoint");
			_currentGameIndex = serializedObject.FindProperty("currentGame");
			_availableGames = serializedObject.FindProperty("availableGames");

			_elympicsWebEndpointChecker = new EditorEndpointChecker();
			_elympicsWebEndpointChecker.UpdateUri(_elympicsWebEndpoint.stringValue);

			var chosenGameProperty = GetChosenGameProperty();
			CreateChosenGameEditorIfChanged(chosenGameProperty);

			_indentation = new GUIStyle {margin = new RectOffset(10, 0, 0, 0)};
			_labelWidthStack = new Stack<float>();

			Undo.undoRedoPerformed += UpdateUriOnUndoRedo;
			EditorApplication.update += EditorUpdate;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UpdateUriOnUndoRedo;
		}

		private void EditorUpdate()
		{
			_elympicsWebEndpointChecker.Update();
		}

		private void UpdateUriOnUndoRedo()
		{
			serializedObject.Update();
			_elympicsWebEndpointChecker.UpdateUri(_elympicsWebEndpoint.stringValue);
		}

		private SerializedProperty GetChosenGameProperty()
		{
			var chosen = _currentGameIndex.intValue;
			if (chosen < 0 || chosen >= _availableGames.arraySize)
				return null;

			return _availableGames.GetArrayElementAtIndex(chosen);
		}

		private void CreateChosenGameEditorIfChanged(SerializedProperty chosenGameProperty)
		{
			if (chosenGameProperty.objectReferenceValue == _lastChosenGamePropertyObject)
				return;

			if (_lastChosenGameEditor != null)
				DestroyImmediate(_lastChosenGameEditor);
			_lastChosenGameEditor = CreateEditor(chosenGameProperty.objectReferenceValue);
			_lastChosenGamePropertyObject = chosenGameProperty.objectReferenceValue;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorStyles.label.wordWrap = true;

			DrawLoginSection();
			EditorGUILayout.PropertyField(_availableGames, new GUIContent("Available games"), true);

			_currentGameIndex.intValue = EditorGUILayout.Popup(new GUIContent("Active game"), _currentGameIndex.intValue, ((List<ElympicsGameConfig>) _availableGames.GetValue()).Select(x => $"{x?.GameName} - {x?.GameId}").ToArray());

			var chosenGameProperty = GetChosenGameProperty();

			DrawTitle(chosenGameProperty);
			DrawGameEditor(chosenGameProperty);

			serializedObject.ApplyModifiedProperties();
		}

		public void DrawLoginSection()
		{
			EditorEndpointCheckerEditor.DrawEndpointField(serializedObject, _elympicsWebEndpointChecker, _elympicsWebEndpoint, "ElympicsWeb endpoint");

			if (EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
			{
				EditorGUILayout.LabelField($"Logged in ElympicsWeb as {EditorPrefs.GetString(ElympicsConfig.UsernameKey)}");

				if (GUILayout.Button("Logout", GUILayout.Width(100), GUILayout.MaxHeight(20)))
				{
					EditorPrefs.SetBool(ElympicsConfig.IsLoginKey, false);
					EditorPrefs.SetString(ElympicsConfig.PasswordKey, string.Empty);
					EditorPrefs.SetString(ElympicsConfig.RefreshTokenKey, string.Empty);
					EditorPrefs.SetString(ElympicsConfig.AuthTokenKey, string.Empty);
					EditorPrefs.SetString(ElympicsConfig.UsernameKey, string.Empty);
					GUI.FocusControl(null);
					Debug.Log("Logged out from Elympics");
				}
			}
			else
			{
				BeginSection("Login");
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(ElympicsConfig.UsernameKey);
				EditorPrefs.SetString(ElympicsConfig.UsernameKey, EditorGUILayout.TextField(EditorPrefs.GetString(ElympicsConfig.UsernameKey)));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(ElympicsConfig.PasswordKey);
				EditorPrefs.SetString(ElympicsConfig.PasswordKey, EditorGUILayout.TextField(EditorPrefs.GetString(ElympicsConfig.PasswordKey)));
				GUILayout.EndHorizontal();
				EndSection();

				EditorGUILayout.Separator();

				if (GUILayout.Button("Login", GUILayout.Width(100), GUILayout.MaxHeight(20)))
				{
					if (!_elympicsWebEndpointChecker.IsRequestSuccessful)
					{
						Debug.LogError("Cannot connect with ElympicsWeb, check ElympicsWeb endpoint");
						return;
					}

					ElympicsWebIntegration.Login();
				}
			}

			serializedObject.Update();
			serializedObject.ApplyModifiedProperties();
			EditorGUILayout.Separator();
		}

		private static void DrawTitle(SerializedProperty chosenGameProperty)
		{
			var labelStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 20,
				alignment = TextAnchor.MiddleLeft
			};
			EditorGUILayout.Separator();
			EditorGUILayout.Space();
			var elympicsGameConfig = (ElympicsGameConfig) chosenGameProperty.GetValue();
			EditorGUILayout.LabelField($"{elympicsGameConfig.GameName}", labelStyle);
			EditorGUILayout.Space();
		}

		private void DrawGameEditor(SerializedProperty chosenGameProperty)
		{
			var gameStyle = new GUIStyle {margin = new RectOffset(10, 0, 0, 0)};
			EditorGUILayout.BeginVertical(gameStyle);
			CreateChosenGameEditorIfChanged(chosenGameProperty);
			_lastChosenGameEditor.OnInspectorGUI();
			EditorGUILayout.EndVertical();
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
	}
}