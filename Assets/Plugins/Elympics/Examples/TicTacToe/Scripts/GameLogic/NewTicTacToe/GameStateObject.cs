using System.Collections.Generic;
using UnityEngine;
using Elympics;

namespace GameLogic.NewTicTacToe
{
	public class GameStateObject : MonoBehaviour, IObservable
	{
		[SerializeField] private GameObject            gameOverObject        = null;
		[SerializeField] private AsyncEventsDispatcher asyncEventsDispatcher = null;

		#region GameState

		[SerializeField] public  List<Field>  fields            = null;
		[SerializeField] private ElympicsInt  player1Side       = new ElympicsInt(0);
		[SerializeField] private ElympicsInt  player2Side       = new ElympicsInt(0);
		[SerializeField] private ElympicsInt  currentPlayerTurn = new ElympicsInt(ElympicsPlayer.INVALID_ID);
		[SerializeField] private ElympicsBool gameOver          = new ElympicsBool(false);

		#endregion

		private void Awake()
		{
			gameOver.ValueChanged += HandleGameOverChanged;
		}

		private void HandleGameOverChanged(bool lastValue, bool newValue)
		{
			asyncEventsDispatcher.Enqueue(() => gameOverObject.SetActive(newValue));
		}

		public void SetSides(PlayerSide player1Side, PlayerSide player2Side)
		{
			this.player1Side.Value = (int) player1Side;
			this.player2Side.Value = (int) player2Side;
		}

		public PlayerSide GetSide(int playerId)
		{
			if (playerId == ElympicsPlayer.GetPlayerId(0))
				return (PlayerSide) player1Side.Value;
			if (playerId == ElympicsPlayer.GetPlayerId(1))
				return (PlayerSide) player2Side.Value;
			return PlayerSide.None;
		}

		public bool IsPlayersTurn(int playerType) => playerType == currentPlayerTurn;

		public void SetTurnForPlayer(int playerId) => currentPlayerTurn.Value = playerId;

		public void ChangeTurn() => currentPlayerTurn.Value = ElympicsPlayer.PLAYER1_ID + 1 - currentPlayerTurn;

		public void SetGameOver(bool value) => gameOver.Value = true;

		public bool IsGameOver() => gameOver;
	}
}
