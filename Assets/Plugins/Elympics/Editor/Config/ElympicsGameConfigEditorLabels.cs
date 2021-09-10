namespace Elympics
{
	internal partial class ElympicsGameConfigEditor
	{
		private const string Label_BotsInsideServerSummary = "Defines if server should run bots internally instead of independent processes. " +
		                                                     "Running bots independently requires a lot more resources, but allows to change internal state without consequences.";

		private const string Label_CreateGameSummary = "Create new game in Elympics with current name and overwrite current config with new game id. It's required to first created game before upload a new version";

		private const string Label_UploadSummary = "Upload new version of game with current settings to Elympics, game name and game id in config should match with game in Elympics. " +
		                                           "It's required to first upload a game version if you want to play it in online mode.";
	}
}