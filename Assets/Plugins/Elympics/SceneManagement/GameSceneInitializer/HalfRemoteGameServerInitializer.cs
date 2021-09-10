using GameEngineCore.V1._3;
using UnityConnectors;

namespace Elympics
{
	internal class HalfRemoteGameServerInitializer : GameServerInitializer
	{
		private HalfRemoteGameEngineProtoConnector _halfRemoteGameEngineProtoConnector;

		protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
		{
			_halfRemoteGameEngineProtoConnector = new HalfRemoteGameEngineProtoConnector(elympicsGameConfig.PortForHalfRemoteMode, gameEngineAdapter);
			
			var initialMatchData = new InitialMatchUserDatas(DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig));
			gameEngineAdapter.Init(new LoggerNoop(), null);
			gameEngineAdapter.Init2(initialMatchData);
			_halfRemoteGameEngineProtoConnector.Listen();
		}

		public override void Dispose()
		{
			base.Dispose();
			_halfRemoteGameEngineProtoConnector?.Dispose();
		}
	}
}
