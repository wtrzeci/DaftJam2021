using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elympics
{
	[CreateAssetMenu(fileName = "ElympicsConfig", menuName = "Elympics/Config")]
	public class ElympicsConfig : ScriptableObject
	{
		internal const string PATH_IN_RESOURCES = "Elympics/ElympicsConfig";

		#region ElympicsPrefs Keys

		public const string UsernameKey     = "Username";
		public const string PasswordKey     = "Password";
		public const string RefreshTokenKey = "RefreshToken";
		public const string AuthTokenKey    = "AuthToken";
		public const string IsLoginKey      = "IsLogin";

		#endregion

		[SerializeField] private string elympicsWebEndpoint = "https://api.elympics.cc";

		internal string ElympicsWebEndpoint => elympicsWebEndpoint;

		internal static ElympicsConfig Load() => Resources.Load<ElympicsConfig>(PATH_IN_RESOURCES);

		internal static ElympicsGameConfig LoadCurrentElympicsGameConfig()
		{
			var elympicsConfig = Resources.Load<ElympicsConfig>(PATH_IN_RESOURCES);
			if (elympicsConfig.currentGame == -1)
				throw new NullReferenceException("Choose game config in ElympicsConfig!");
			if (elympicsConfig.currentGame < 0 || elympicsConfig.currentGame >= elympicsConfig.availableGames.Count)
				throw new NullReferenceException("Game config out of range in ElympicsConfig!");
			return elympicsConfig.availableGames[elympicsConfig.currentGame];
		}

		[SerializeField] internal int                      currentGame = -1;
		[SerializeField] internal List<ElympicsGameConfig> availableGames;
	}
}