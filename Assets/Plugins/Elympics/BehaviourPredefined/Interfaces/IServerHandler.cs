namespace Elympics
{
	public interface IServerHandler : IObservable
	{
		/// <summary>
		/// Called on server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
		/// </summary>
		/// <param name="initialMatchPlayerDatas">Initialization data of all possible clients and bots.</param>
		void OnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas);

		/// <summary>
		/// Called when a client/bot disconnects from the server.
		/// </summary>
		/// <param name="playerId">Identifier of disconnected player.</param>
		void OnPlayerDisconnected(int playerId);

		/// <summary>
		/// Called when a client/bot connects to the server.
		/// </summary>
		/// <param name="playerId">Identifier of connected player.</param>
		void OnPlayerConnected(int playerId);
	}
}
