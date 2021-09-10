using Elympics;
using UnityEngine;

public class HelloWorldInputController : MonoBehaviour, IInputHandler
{
	[SerializeField]
	private GameObject[] labels = null;

	private bool _clicked;

	public void GetInputForClient(IInputWriter inputSerializer)
	{
		inputSerializer.Write(_clicked);
		_clicked = false;
	}

	public void GetInputForBot(IInputWriter inputSerializer)
	{
	}

	public void ApplyInput(int playerId, IInputReader inputReader)
	{
		inputReader.Read(out bool value);
		if (value)
			labels[playerId].SetActive(value);
	}

	public void OnClick()
	{
		_clicked = true;
	}
}
