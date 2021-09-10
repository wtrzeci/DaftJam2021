using UnityEngine;
using UnityEngine.UI;

public class Player2DInputProvider : MonoBehaviour
{
	[SerializeField] private Text debugText = null;

	public void GetRawInput(out float movement, out bool fire, out bool jump)
	{
		movement = Input.GetAxis("Horizontal");
		fire = Input.GetAxis("Fire1") != 0;
		jump = Input.GetAxis("Jump") != 0;
		if (debugText != null)
			debugText.text = $"{movement} {fire} {jump}";
	}
}
