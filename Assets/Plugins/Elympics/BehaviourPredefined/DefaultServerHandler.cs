using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
	public class DefaultServerHandler : ElympicsMonoBehaviour, IServerHandler
	{
		private          int          _playersNumber;
		private readonly HashSet<int> _playersConnected = new HashSet<int>();

		private static readonly TimeSpan StartGameTimeout = TimeSpan.FromSeconds(30);
		private                 DateTime _waitToStartFinishTime;
		private                 bool     _gameStarted;

		public void OnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas)
		{
			if (!enabled)
				return;

			_playersNumber = initialMatchPlayerDatas.Count;
			var humansPlayers = initialMatchPlayerDatas.Count(x => !x.IsBot);
			Debug.Log($"Game initialized with {humansPlayers} human players and {initialMatchPlayerDatas.Count - humansPlayers} bots");

			StartCoroutine(WaitForGameStartOrEnd());
		}

		private IEnumerator WaitForGameStartOrEnd()
		{
			_waitToStartFinishTime = DateTime.Now + StartGameTimeout;

			while (DateTime.Now < _waitToStartFinishTime)
			{
				Debug.Log("Waiting for game to start");
				if (_gameStarted)
				{
					Debug.Log("Game started!");
					yield break;
				}

				yield return new WaitForSeconds(5);
			}

			Debug.Log("Forcing game end because game didn't start");
			Elympics.EndGame();
		}

		public void OnPlayerDisconnected(int playerId)
		{
			if (!enabled)
				return;

			Debug.Log($"Player {playerId} disconnected");
			Debug.Log("Game ended!");
			Elympics.EndGame();
		}

		public void OnPlayerConnected(int playerId)
		{
			if (!enabled)
				return;

			Debug.Log($"Player {playerId} connected");

			_playersConnected.Add(playerId);
			if (_playersConnected.Count != _playersNumber || _gameStarted)
				return;

			_gameStarted = true;
			Elympics.StartGame();
		}
	}
}
