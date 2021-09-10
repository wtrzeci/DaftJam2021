using UnityEngine;

namespace Elympics
{
	[RequireComponent(typeof(ElympicsBehaviour))]
	public class ElympicsMonoBehaviour : MonoBehaviour
	{
		private IElympics       _elympics;
		private ElympicsFactory _factory;

		/// <summary>
		/// Provides Elympics-specific game instance data and methods.
		/// </summary>
		/// <exception cref="MissingComponentException">ElympicsBehaviour component is not attached to the object.</exception>
		public IElympics Elympics
		{
			get
			{
				if (_elympics != null)
					return _elympics;

				if (TryGetComponent(out ElympicsBehaviour elympicsBehaviour))
				{
					_elympics = elympicsBehaviour.Controller;
					return _elympics;
				}

				var missingComponentMessage = $"{GetType().Name} has missing {nameof(ElympicsBehaviour)} component";
				Debug.LogError(missingComponentMessage, this);
				throw new MissingComponentException(missingComponentMessage);
			}
		}

		/// <summary>
		/// Synchronize a prefab instantiation and process all its ElympicsBehaviour components.
		/// </summary>
		/// <param name="pathInResources">Path to instantiated prefab which must reside in Resources.</param>
		/// <returns>Created game object.</returns>
		/// <remarks>For object destruction see <see cref="ElympicsDestroy"/>.</remarks>
		public GameObject ElympicsInstantiate(string pathInResources)
			=> GetFactoryOnTheSameScene().CreateInstance(pathInResources);

		private ElympicsFactory GetFactoryOnTheSameScene()
			=> _factory ?? (_factory = ElympicsFactory.GetInstanceOnScene(gameObject.scene));

		/// <summary>
		/// Synchronize a game object destruction.
		/// </summary>
		/// <param name="createdGameObject">Destroyed game object.</param>
		/// <remarks>Only objects instantiated with <see cref="ElympicsInstantiate"/> may be destroyed with this method.</remarks>
		public void ElympicsDestroy(GameObject createdGameObject)
		{
			var factory = ElympicsFactory.GetInstanceOnScene(createdGameObject.scene);
			factory.DestroyInstance(createdGameObject);
		}
	}
}
