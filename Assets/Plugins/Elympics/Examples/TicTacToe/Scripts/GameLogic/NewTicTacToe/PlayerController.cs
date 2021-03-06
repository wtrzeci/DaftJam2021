using System.Collections.Generic;
using UnityEngine;
using Elympics;
using MatchTcpClients.Synchronizer;

namespace GameLogic.NewTicTacToe
{
	public class PlayerController : ElympicsMonoBehaviour, IInputHandler, IClientHandler, IBotHandler
	{
		[SerializeField] public GameStateObject     gameState           = null;
		[SerializeField] public PlayerInputProvider playerInputProvider = null;

		private readonly Dictionary<int, BotInputProvider> _botControllers = new Dictionary<int, BotInputProvider>();

		private PlayerSide MyPlayerSide => gameState.GetSide(Elympics.PlayerId);

		public void GetInputForClient(IInputWriter inputWriter)
		{
			var input = playerInputProvider.GetAndClearLastClickedInput();
			SerializeInput(inputWriter, input);
		}

		// Handling all bots through this input handler
		public void GetInputForBot(IInputWriter inputSerializer)
		{
			var input = _botControllers[Elympics.PlayerId].ChosenField;
			SerializeInput(inputSerializer, input);
		}

		private void SerializeInput(IInputWriter inputSerializer, Input input)
		{
			if (input.Equals(Input.Empty))
				return;

			inputSerializer.Write(input.FieldIndex);
			inputSerializer.Write(MyPlayerSide);
		}

		public void ApplyInput(int playerId, IInputReader inputReader)
		{
			inputReader.Read(out int fieldIndex);
			inputReader.Read(out PlayerSide playerSide);

			var validated = Validate(playerId, fieldIndex, playerSide);
			if (!validated)
				return;

			gameState.fields[fieldIndex].SetOwnership(playerSide);
			gameState.fields[fieldIndex].transform.localPosition += new Vector3(100, 0, 0);
			gameState.ChangeTurn();
		}

		private bool Validate(int playerId, int fieldIndex, PlayerSide playerSide)
		{
			return gameState.fields[fieldIndex].Ownership == PlayerSide.None
			       && gameState.GetSide(playerId) == playerSide
			       && gameState.IsPlayersTurn(playerId)
			       && !gameState.IsGameOver();
		}

		public void OnDisconnectedByServer()
		{
			if (gameState.IsGameOver())
				return;
			// Auto reconnect
			Elympics.ConnectAndJoinAsPlayer().ContinueWith(x =>
			{
				if (gameState.IsGameOver())
					return;

				if (x.IsFaulted)
					gameState.SetGameOver(true);
				else if (x.IsCompleted && !x.Result)
					gameState.SetGameOver(true);
			});
		}

		public void OnStandaloneBotInit(InitialMatchPlayerData initialMatchPlayerData)
		{
			var botController = gameObject.AddComponent<BotInputProvider>();
			botController.gameState = gameState;
			botController.playerId = initialMatchPlayerData.PlayerId;

			_botControllers.Add(initialMatchPlayerData.PlayerId, botController);
		}

		public void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas)
		{
			foreach (var initialMatchPlayerData in initialMatchPlayerDatas)
				OnStandaloneBotInit(initialMatchPlayerData);
		}

		public void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
		}

		public void OnClientsOnServerInit(InitialMatchPlayerDatas data)
		{
		}

		public void OnDisconnectedByClient()
		{
		}

		public void OnConnected(TimeSynchronizationData data)
		{
			Debug.Log($"Connected with RTT {data.RoundTripDelay}");
		}

		public void OnConnectingFailed()
		{
			Debug.Log($"Connecting failed");
		}

		public void OnSynchronized(TimeSynchronizationData data)
		{
			// Debug.Log($"Synchronized {data.RoundTripDelay}");
		}

		public void OnAuthenticated(string userId)
		{
			Debug.Log($"Authenticated with userId {userId}");
		}

		public void OnAuthenticatedFailed(string errorMessage)
		{
			Debug.Log($"Authentication failed - {errorMessage}");
		}

		public void OnMatchJoined(string matchId)
		{
			Debug.Log($"Match joined");
		}

		public void OnMatchJoinedFailed(string errorMessage)
		{
			Debug.Log($"Match joining failed");
		}

		public void OnMatchEnded(string matchId)
		{
			Debug.Log($"Match ended");
			gameState.SetGameOver(true);
		}
	}
}
