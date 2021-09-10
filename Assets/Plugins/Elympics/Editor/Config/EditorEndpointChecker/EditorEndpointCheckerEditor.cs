using UnityEditor;
using UnityEngine;

namespace Elympics
{
	public static class EditorEndpointCheckerEditor
	{
		public static void DrawEndpointField(SerializedObject serializedObject, EditorEndpointChecker editorEndpointChecker, SerializedProperty endpoint, string label)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(endpoint, new GUIContent(label), GUILayout.MaxWidth(float.MaxValue));
			serializedObject.ApplyModifiedProperties();
			if (EditorGUI.EndChangeCheck())
				editorEndpointChecker.UpdateUri(endpoint.stringValue);

			if (!Application.isPlaying)
				DrawEndpointIndicator(editorEndpointChecker);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();
		}

		private static void DrawEndpointIndicator(EditorEndpointChecker endpointChecker)
		{
			string text;
			Color color;

			if (!endpointChecker.IsUriCorrect)
			{
				text = "Wrong uri";
				color = Color.yellow;
			}
			else if (!endpointChecker.IsRequestDone)
			{
				text = "Connecting...";
				color = Color.blue;
			}
			else if (!endpointChecker.IsRequestSuccessful)
			{
				text = "Didn't connect";
				color = Color.red;
			}
			else
			{
				text = "Connected!";
				color = Color.green;
			}

			var originalColor = GUI.color;
			GUI.color = color;
			EditorGUILayout.LabelField(text, GUILayout.Width(90));
			GUI.color = originalColor;
		}
	}
}
