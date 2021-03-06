using System.Threading.Tasks;

namespace Elympics
{
	public interface IElympics
	{
		/// <value>Current player identifier.</value>
		/// <remarks>If client or bot is handled in server (e.g. in "Local Player And Bots" mode), their ID is only available during input gathering and game
		/// initialization.</remarks>
		int   PlayerId     { get; }

		/// <value>Is game instance a server?</value>
		/// <remarks>If client or bot is handled in server (e.g. in "Local Player And Bots" mode), the property is always truthy for them.</remarks>
		bool  IsServer     { get; }

		/// <value>Is game instance a client?</value>
		/// <remarks>If client is handled in server (in "Local Player And Bots" mode), the property is only meaningful during input gathering and game
		/// initialization.</remarks>
		bool  IsClient     { get; }

		/// <value>Is game instance a bot?</value>
		/// <remarks>If bot is handled in server (in "Local Player And Bots" mode or with "Bots inside server" option checked), the property is only meaningful
		/// during input gathering and game initialization.</remarks>
		bool  IsBot        { get; }

		/// <value>The interval in seconds at which network synchronization occurs. It is equal to <see cref="UnityEngine.Time.fixedDeltaTime"/>.</value>
		float TickDuration { get; }

		#region Client

		Task<bool> ConnectAndJoinAsPlayer();
		Task<bool> ConnectAndJoinAsSpectator();
		void       Disconnect();

		#endregion

		#region Server

		void StartGame();
		void EndGame(ResultMatchPlayerDatas result = null);

		#endregion
	}
}
