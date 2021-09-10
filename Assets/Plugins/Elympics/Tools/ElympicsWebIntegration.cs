#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Web;
using ElympicsApiModels.ApiModels.Auth;
using ElympicsApiModels.ApiModels.Games;
using UnityEditor;
using UnityEngine;
using GamesRoutes = ElympicsApiModels.ApiModels.Games.Routes;
using AuthRoutes = ElympicsApiModels.ApiModels.Auth.Routes;

namespace Elympics
{
	public static class ElympicsWebIntegration
	{
		private const string PackFileName          = "pack.zip";
		private const string DirectoryNameToUpload = "Games";
		private const string EngineSubdirectory    = "Engine";
		private const string BotSubdirectory       = "Bot";

		private static          string                ElympicsWebEndpoint => ElympicsConfig.Load().ElympicsWebEndpoint;
		private static readonly JsonContentSerializer Serializer = new JsonContentSerializer();

		public static void Login()
		{
			var request = new LoginUserNamePasswordRequestModel
			{
				UserName = EditorPrefs.GetString(ElympicsConfig.UsernameKey),
				Password = EditorPrefs.GetString(ElympicsConfig.PasswordKey)
			};

			var uri = GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.LoginRoute);
			var response = ElympicsWebClient.SendJsonPostRequest(uri, Serializer.Serialize(request));

			response.completed += _ =>
			{
				if (ElympicsWebClient.TryDeserializeResponse(response, "Login", out LoggedInTokenResponseModel responseModel))
				{
					EditorPrefs.SetBool(ElympicsConfig.IsLoginKey, true);
					EditorPrefs.SetString(ElympicsConfig.AuthTokenKey, responseModel.AuthToken);
					EditorPrefs.SetString(ElympicsConfig.RefreshTokenKey, responseModel.RefreshToken);
					Debug.Log($"Logged to ElympicsWeb as {responseModel.UserName}");
				}
			};
		}

		public static void CreateGame(SerializedProperty gameName, SerializedProperty gameId)
		{
			var request = new CreateGameRequestModel
			{
				GameName = gameName.stringValue
			};

			var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute);
			var response = ElympicsWebClient.SendJsonPostRequest(url, Serializer.Serialize(request));

			response.completed += _ =>
			{
				if (ElympicsWebClient.TryDeserializeResponse(response, "Create game", out GameResponseModel responseModel))
				{
					gameId.SetValue(responseModel.Id);
					gameName.SetValue(responseModel.Name);
					Debug.Log($"Created game {responseModel.Name} with id {responseModel.Id}");
				}
			};
		}

		public static void UploadGame()
		{
			BuildTools.BuildServerLinux();

			try
			{
				var title = "Uploading to Elympics Cloud";
				EditorUtility.DisplayProgressBar(title, "", 0f);
				var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
				if (!EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
				{
					Debug.LogError("You must be logged in Elympics to upload games");
					return;
				}

				EditorUtility.DisplayProgressBar(title, "Packing engine", 0.2f);
				if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out string enginePath))
					return;

				EditorUtility.DisplayProgressBar(title, "Packing bot", 0.4f);
				if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.BotPath, BotSubdirectory, out string botPath))
					return;

				var queryParamsDict = new Dictionary<string, string>
				{
					["gameId"] = currentGameConfig.GameId,
					["gameVersion"] = currentGameConfig.GameVersion
				};

				EditorUtility.DisplayProgressBar(title, "Uploading...", 0.8f);
				var url = AppendQueryParamsToUrl(GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, GamesRoutes.GamesUploadRoute), queryParamsDict);
				var response = ElympicsWebClient.SendEnginePostRequest(url, new[] {enginePath, botPath});

				response.completed += _ =>
				{
					if (response.webRequest.responseCode != 200)
					{
						ElympicsWebClient.LogResponseErrors("Upload game version", response.webRequest);
						return;
					}

					Debug.Log($"Uploaded {currentGameConfig.GameName} with version {currentGameConfig.GameVersion}");
				};
				
				EditorUtility.DisplayProgressBar(title, "Uploaded", 1f);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
			
		}

		private static bool TryPack(string gameId, string gameVersion, string buildPath, string targetSubdirectory, out string destinationFilePath)
		{
			var destinationDirectoryPath = Path.Combine(DirectoryNameToUpload, gameId, gameVersion, targetSubdirectory);
			destinationFilePath = Path.Combine(destinationDirectoryPath, PackFileName);

			Directory.CreateDirectory(destinationDirectoryPath);
			try
			{
				Debug.Log($"Trying to pack {targetSubdirectory}");
				if (File.Exists(destinationFilePath))
					File.Delete(destinationFilePath);
				ZipFile.CreateFromDirectory(buildPath, destinationFilePath, System.IO.Compression.CompressionLevel.Optimal, false);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				return false;
			}

			return true;
		}

		private static string AppendQueryParamsToUrl(string url, Dictionary<string, string> queryParamsDict)
		{
			var uriBuilder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			foreach (var queryParam in queryParamsDict)
				query.Add(queryParam.Key, queryParam.Value);
			uriBuilder.Query = query.ToString();
			return uriBuilder.ToString();
		}

		private static string GetCombinedUrl(params string[] urlParts) => string.Join("/", urlParts);
	}
}
#endif