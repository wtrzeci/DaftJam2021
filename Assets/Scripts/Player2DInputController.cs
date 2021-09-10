using UnityEngine;
using Elympics;

public class Player2DInputController : ElympicsMonoBehaviour, IInitializable, IInputHandler
{
	[SerializeField] private Player2DInputProvider inputProvider = null;
	[SerializeField] private Player2DBehaviour _playerBehaviour;

	public void GetInputForClient(IInputWriter inputSerializer)
	{
		inputProvider.GetRawInput(out var movement, out var fire, out var jump);
		SerializeInput(inputSerializer, movement, fire, jump);
	}

	public void GetInputForBot(IInputWriter inputSerializer)
	{
		// if you want to implement bots, you can take and serialize their inputs here
	}

	private static void SerializeInput(IInputWriter inputWriter, float movement, bool fire, bool jump)
	{
		// write all input variables, you can add more if you want but remember to read all variables in ApplyInput
		inputWriter.Write(movement);
		inputWriter.Write(fire);
		inputWriter.Write(jump);
	}

	public void ApplyInput(int playerId, IInputReader inputReader)
	{
		// remember to read all input variables in the same order they were written in
		inputReader.Read(out float movement);
		inputReader.Read(out bool fire);
		inputReader.Read(out bool jump);

		// check ownership of the player behaviour
		if (playerId != _playerBehaviour.associatedPlayerId)
			return;

		// apply inputs to player behaviour
		if (fire)
			_playerBehaviour.Fire();
		_playerBehaviour.Move(movement);
		if (jump)
			_playerBehaviour.Jump();

		// you can add and apply any other input you wish here
	}

	public void Initialize()
	{
		_playerBehaviour = GetComponent<Player2DBehaviour>();
	}
}
