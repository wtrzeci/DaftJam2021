using System;
using System.Threading.Tasks;
using GameEngineCore.V1._3;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	public abstract class ElympicsBase : MonoBehaviour, IElympics
	{
		[SerializeField] internal ElympicsBehavioursManager elympicsBehavioursManager = null;
		[SerializeField] internal ElympicsLateFixedUpdate   elympicsLateFixedUpdate   = null;
		[SerializeField] internal AsyncEventsDispatcher     asyncEventsDispatcher     = null;

		[Tooltip("Attach gameobjects that you want to be destroyed together with this system")] [SerializeField]
		private GameObject[] linkedLogic = null;

		private protected ElympicsGameConfig Config;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig)
		{
			Config = elympicsGameConfig;
			Time.fixedDeltaTime = elympicsGameConfig.TickDuration;
		}

		public void Destroy()
		{
			foreach (var linkedLogicObject in linkedLogic)
				DestroyImmediate(linkedLogicObject);
			DestroyImmediate(gameObject);
		}

		private void FixedUpdate()
		{
			if (!ShouldDoFixedUpdate())
				return;

			DoFixedUpdate();
			elympicsBehavioursManager.PredictableFixedUpdate();
			elympicsLateFixedUpdate.LateFixedUpdateAction = LateFixedUpdate;
		}

		protected virtual  bool ShouldDoFixedUpdate() => true;
		protected abstract void DoFixedUpdate();

		protected virtual void LateFixedUpdate()
		{
		}

		#region ConnectionCallbacks

		protected void OnStandaloneClientInit(InitialMatchPlayerData data) => Enqueue(() => elympicsBehavioursManager.OnStandaloneClientInit(data));
		protected void OnClientsOnServerInit(InitialMatchPlayerDatas data) => Enqueue(() => elympicsBehavioursManager.OnClientsOnServerInit(data));
		protected void OnSynchronized(TimeSynchronizationData data)        => Enqueue(() => elympicsBehavioursManager.OnSynchronized(data));
		protected void OnDisconnectedByServer()                            => Enqueue(elympicsBehavioursManager.OnDisconnectedByServer);
		protected void OnDisconnectedByClient()                            => Enqueue(elympicsBehavioursManager.OnDisconnectedByClient);
		protected void OnConnected(TimeSynchronizationData data)           => Enqueue(() => elympicsBehavioursManager.OnConnected(data));
		protected void OnConnectingFailed()                                => Enqueue(elympicsBehavioursManager.OnConnectingFailed);
		protected void OnAuthenticated(string userId)                      => Enqueue(() => elympicsBehavioursManager.OnAuthenticated(userId));
		protected void OnAuthenticatedFailed(string errorMessage)          => Enqueue(() => elympicsBehavioursManager.OnAuthenticatedFailed(errorMessage));
		protected void OnMatchJoined(string matchId)                       => Enqueue(() => elympicsBehavioursManager.OnMatchJoined(matchId));
		protected void OnMatchEnded(string matchId)                        => Enqueue(() => elympicsBehavioursManager.OnMatchEnded(matchId));
		protected void OnMatchJoinedFailed(string errorMessage)            => Enqueue(() => elympicsBehavioursManager.OnMatchJoinedFailed(errorMessage));

		#endregion

		#region BotCallbacks

		protected void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnStandaloneBotInit(initialMatchData));
		protected void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnBotsOnServerInit(initialMatchData));

		#endregion

		#region ServerCallbacks

		protected void OnServerInit(InitialMatchPlayerDatas initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnServerInit(initialMatchData));
		protected void OnPlayerConnected(int playerId)                        => Enqueue(() => elympicsBehavioursManager.OnPlayerConnected(playerId));
		protected void OnPlayerDisconnected(int playerId)                     => Enqueue(() => elympicsBehavioursManager.OnPlayerDisconnected(playerId));

		#endregion

		protected void Enqueue(Action action) => asyncEventsDispatcher.Enqueue(action);


		#region IElympics

		public virtual int   PlayerId     { get; } = ElympicsPlayer.INVALID_ID;
		public virtual bool  IsBot        { get; } = false;
		public virtual bool  IsServer     { get; } = false;
		public virtual bool  IsClient     { get; } = false;
		public         float TickDuration => Config.TickDuration;

		#region Client

		public virtual Task<bool> ConnectAndJoinAsPlayer()    => throw new NotImplementedException("This method is supported only for client");
		public virtual Task<bool> ConnectAndJoinAsSpectator() => throw new NotImplementedException("This method is supported only for client");
		public virtual void       Disconnect()                => throw new NotImplementedException("This method is supported only for client");

		#endregion

		#region Server

		public virtual void StartGame()                                   => throw new NotImplementedException("This method is supported only for server");
		public virtual void EndGame(ResultMatchPlayerDatas result = null) => throw new NotImplementedException("This method is supported only for server");

		#endregion

		#endregion
	}
}
