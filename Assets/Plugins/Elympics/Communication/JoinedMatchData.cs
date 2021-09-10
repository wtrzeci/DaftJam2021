using System.Collections.Generic;

namespace Elympics
{
	public class JoinedMatchData
	{
		public JoinedMatchData(string matchId, string serverAddress, string userSecret, List<string> matchedPlayers, float[] matchmakerData, byte[] gameEngineData)
		{
			MatchId = matchId;
			ServerAddress = serverAddress;
			UserSecret = userSecret;
			MatchedPlayers = matchedPlayers;
			MatchmakerData = matchmakerData;
			GameEngineData = gameEngineData;
		}

		public string       MatchId        { get; }
		public string       ServerAddress  { get; }
		public string       UserSecret     { get; }
		public List<string> MatchedPlayers { get; }
		public float[]      MatchmakerData { get; }
		public byte[]       GameEngineData { get; }
	}
}
