namespace Elympics
{
	public static class ElympicsPlayer
	{
		public const int WORLD_ID   = -2;
		public const int INVALID_ID = -1;
		public const int PLAYER1_ID = 0;

		public static int GetPlayerId(int playerNumber) => PLAYER1_ID + playerNumber;
	}
}
