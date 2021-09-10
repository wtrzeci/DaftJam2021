using System.Collections.Generic;

namespace Elympics
{
	public static class ElympicsPlayerAssociations
	{
		public static Dictionary<string, int> GetPlayerAssociations(List<string> playerIds)
		{
			var playerTypes = new Dictionary<string, int>();
			for (var i = 0; i < playerIds.Count; i++)
				playerTypes[playerIds[i]] = ElympicsPlayer.GetPlayerId(i);
			return playerTypes;
		}
	}
}
