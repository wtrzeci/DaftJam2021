#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElympicsApiModels.ApiModels.Games;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsWebClient
	{
		private static readonly JsonContentSerializer _serializer = new JsonContentSerializer();

		public static UnityWebRequestAsyncOperation SendEnginePostRequest(string url, string[] filesPath)
		{
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			foreach (var path in filesPath)
				formData.Add(new MultipartFormFileSection(Routes.GamesUploadRequestFilesFieldName, File.ReadAllBytes(path), "pack.zip", "multipart/form-data"));
			UnityWebRequest request = UnityWebRequest.Post(url, formData);
			request.SetRequestHeader("Authorization", $"Bearer {EditorPrefs.GetString(ElympicsConfig.AuthTokenKey)}");
			return request.SendWebRequest();
		}

		public static UnityWebRequestAsyncOperation SendJsonGetRequest(string url)
		{
			UnityWebRequest request = UnityWebRequest.Get(url);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Authorization", $"Bearer {EditorPrefs.GetString(ElympicsConfig.AuthTokenKey)}");
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			return request.SendWebRequest();
		}

		public static UnityWebRequestAsyncOperation SendJsonPostRequest(string url, string bodyJsonString)
		{
			UnityWebRequest request = UnityWebRequest.Post(url, bodyJsonString);
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Authorization", $"Bearer {EditorPrefs.GetString(ElympicsConfig.AuthTokenKey)}");
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			return request.SendWebRequest();
		}

		public static bool TryDeserializeResponse<T>(UnityWebRequestAsyncOperation response, string actionName, out T deserializedResponse)
		{
			deserializedResponse = default;
			if (response.webRequest.responseCode != 200)
			{
				LogResponseErrors(actionName, response.webRequest);
				return false;
			}

			deserializedResponse = _serializer.Deserialize<T>(response.webRequest.downloadHandler.text);
			return true;
		}

		public static void LogResponseErrors(string actionName, UnityWebRequest request)
		{
			var errorModel = _serializer.Deserialize<ErrorModel>(request.downloadHandler.text);

			if (request.responseCode == 403)
			{
				Debug.LogError("Requested resource is forbidden for this account");
				return;
			}

			if (request.responseCode == 401)
			{
				Debug.LogError("Unauthorized, please login to your ElympicsWeb accout");
				return;
			}

			if (errorModel.Errors != null)
			{
				var errors = string.Join(", ", errorModel.Errors.SelectMany(r => r.Value.Select(x => x)));
				Debug.LogError($"{actionName} failed, {errors}");
			}
		}

		private class ErrorModel
		{
			public Dictionary<string, string[]> Errors { get; set; }
		}
	}
}
#endif