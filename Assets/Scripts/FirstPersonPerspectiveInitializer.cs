using UnityEngine;
using Elympics;
using TechDemo;

public class FirstPersonPerspectiveInitializer : ElympicsMonoBehaviour, IInitializable
{
	[SerializeField] private GameObject body = null;
	[SerializeField] private GameObject weapon = null;
	[SerializeField] private Camera worldCamera = null;
	[SerializeField] private Camera weaponCamera = null;

	public void Initialize()
	{
		var playerBehaviour = GetComponent<PlayerBehaviour>();
		if (playerBehaviour.associatedPlayerId == Elympics.PlayerId
				|| Elympics.IsServer && playerBehaviour.associatedPlayerId == ElympicsPlayer.GetPlayerId(0))
		{
			body.layer = LayerMask.NameToLayer("Invisible");
			weapon.layer = LayerMask.NameToLayer("Weapon");
			worldCamera.gameObject.SetActive(true);
			weaponCamera.gameObject.SetActive(true);
		}
		else
		{
			weapon.layer = LayerMask.NameToLayer("Invisible");
		}
	}
}
