namespace Elympics
{
	public class InitialMatchPlayerData
	{
		public int     PlayerId       { get; set; }
		public string  UserId         { get; set; }
		public bool    IsBot          { get; set; }
		public double  BotDifficulty  { get; set; }
		public byte[]  GameEngineData { get; set; }
		public float[] MatchmakerData { get; set; }
	}
}
