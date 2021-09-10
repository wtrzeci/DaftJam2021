#if UNITY_EDITOR
using System;
using System.IO;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using UnityEngine;
using System.IO.Compression;
using Amazon.Runtime.CredentialManagement;
using UnityEditor;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Elympics
{
	public static class AmazonS3Tools
	{
		private const string BucketName            = "elympics-game-engines";
		private const string PackFileName          = "pack.zip";
		private const string DirectoryNameToUpload = "Games";
		private const string EngineSubdirectory    = "Engine";
		private const string BotSubdirectory       = "Bot";

		public static async void UploadToS3Async()
		{
			var title = "Uploading to S3";
			EditorUtility.DisplayProgressBar(title, "", 0f);
			
			var s3Client = new AmazonS3Client(GetCredentials(), RegionEndpoint.EUCentral1);
			var transferUtility = new TransferUtility(s3Client);
			var config = ElympicsConfig.LoadCurrentElympicsGameConfig();

			EditorUtility.DisplayProgressBar(title, "Packing engine", 0.2f);
			if (!TryPack(config.GameId, config.GameVersion, BuildTools.EnginePath, EngineSubdirectory))
			{
				return;
			}
			EditorUtility.DisplayProgressBar(title, "Packing bot", 0.4f);
			if (!TryPack(config.GameId, config.GameVersion, BuildTools.BotPath, BotSubdirectory))
				return;

			EditorUtility.DisplayProgressBar(title, "Uploading...", 0.8f);
			try
			{
				var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), DirectoryNameToUpload);
				var request = new TransferUtilityUploadDirectoryRequest
				{
					BucketName = BucketName,
					Directory = directoryPath,
					SearchOption = SearchOption.AllDirectories,
					SearchPattern = "*.*",
					KeyPrefix = DirectoryNameToUpload
				};
				await transferUtility.UploadDirectoryAsync(request);
			}
			catch (AmazonS3Exception e)
			{
				Debug.LogError(e.Message);
			}

			EditorUtility.DisplayProgressBar(title, "Uploaded", 1f);
			EditorUtility.ClearProgressBar();
			Debug.Log("Engine and bot uploaded to S3");
		}

		private static bool TryPack(string gameId, string gameVersion, string buildPath, string targetSubdirectory)
		{
			var destinationPath = Path.Combine(DirectoryNameToUpload, gameId, gameVersion, targetSubdirectory);
			var destinationFile = Path.Combine(destinationPath, PackFileName);
			Directory.CreateDirectory(destinationPath);
			try
			{
				if (File.Exists(destinationFile))
					File.Delete(destinationFile);
				ZipFile.CreateFromDirectory(buildPath, destinationFile, CompressionLevel.Optimal, false);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				return false;
			}

			return true;
		}

		private static AWSCredentials GetCredentials()
		{
			try
			{
				return GetDefaultAwsCredentialsFromProfile();
			}
			catch (AmazonClientException e)
			{
				Debug.Log(e.Message);
			}

			try
			{
				return new EnvironmentVariablesAWSCredentials();
			}
			catch (InvalidOperationException e)
			{
				Debug.Log(e.Message);
			}

			throw new AmazonClientException("Not found any AWS credentials");
		}

		private static AWSCredentials GetDefaultAwsCredentialsFromProfile()
		{
			var credentialProfileStoreChain = new CredentialProfileStoreChain();
			if (credentialProfileStoreChain.TryGetAWSCredentials("default", out var defaultCredentials))
				return defaultCredentials;
			throw new AmazonClientException("Unable to find a default profile in CredentialProfileStoreChain.");
		}
	}
}
#endif