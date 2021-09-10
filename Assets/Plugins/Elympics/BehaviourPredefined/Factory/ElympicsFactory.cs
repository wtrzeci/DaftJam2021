using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
	public class ElympicsFactory : MonoBehaviour, IInitializable, IObservable, IStateSerializationHandler
	{
		[SerializeField] private ElympicsBehavioursManager elympicsBehavioursManager = null;

		private static readonly HashSet<ElympicsFactory> AvailableFactories = new HashSet<ElympicsFactory>();

		internal static ElympicsFactory GetInstanceOnScene(Scene scene) => AvailableFactories.FirstOrDefault(factory => factory.gameObject.scene == scene);

		private readonly ElympicsInt                           _currentNetworkId = new ElympicsInt();
		private readonly DynamicElympicsBehaviourInstancesData _instancesData    = new DynamicElympicsBehaviourInstancesData();

		private readonly Dictionary<int, CreatedInstanceWrapper> _createdInstanceWrappersCache = new Dictionary<int, CreatedInstanceWrapper>();
		private readonly Dictionary<GameObject, int>             _createdGameObjectsIds        = new Dictionary<GameObject, int>();

		private NetworkIdEnumerator _networkIdEnumerator;

		public void Initialize()
		{
			_networkIdEnumerator = new NetworkIdEnumerator();
			_currentNetworkId.Value = _networkIdEnumerator.GetCurrent();
		}

		internal GameObject CreateInstance(string pathInResources)
		{
			var instanceId = _instancesData.Add(_networkIdEnumerator.GetCurrent(), pathInResources);
			var instanceWrapper = CreateInstanceInternal(instanceId, pathInResources);
			return instanceWrapper.GameObject;
		}

		private CreatedInstanceWrapper CreateInstanceInternal(int instanceId, string pathInResources)
		{
			SceneManager.SetActiveScene(gameObject.scene);
			var createdPrefab = Resources.Load<GameObject>(pathInResources);
			var createdGameObject = Instantiate(createdPrefab);
			var elympicsBehaviours = createdGameObject.GetComponentsInChildren<ElympicsBehaviour>(true);
			foreach (var behaviour in elympicsBehaviours)
			{
				behaviour.NetworkId = _networkIdEnumerator.MoveNextAndGetCurrent();
				elympicsBehavioursManager.AddNewBehaviour(behaviour);
			}

			var instanceWrapper = new CreatedInstanceWrapper
			{
				GameObject = createdGameObject,
				NetworkIds = elympicsBehaviours.Select(x => x.NetworkId).ToList()
			};

			_createdInstanceWrappersCache.Add(instanceId, instanceWrapper);
			_createdGameObjectsIds.Add(instanceWrapper.GameObject, instanceId);

			return instanceWrapper;
		}

		internal void DestroyInstance(GameObject createGameObject)
		{
			if (!_createdGameObjectsIds.TryGetValue(createGameObject, out var instanceId))
				throw new ArgumentException("Trying to destroy object not created by ElympicsFactory", nameof(createGameObject));

			DestroyInstanceInternal(instanceId);
		}

		private void DestroyInstanceInternal(int instanceId)
		{
			if (!_createdInstanceWrappersCache.TryGetValue(instanceId, out var instance))
				throw new ArgumentException($"Fatal error! Created game object with id {instanceId}, doesn't have cached instance", nameof(instanceId));

			foreach (var instanceNetworkId in instance.NetworkIds)
				elympicsBehavioursManager.RemoveBehaviour(instanceNetworkId);

			_instancesData.Remove(instanceId);
			_createdGameObjectsIds.Remove(instance.GameObject);
			_createdInstanceWrappersCache.Remove(instanceId);

			Destroy(instance.GameObject);
		}

		public void OnPostStateDeserialize()
		{
			if (!_instancesData.AreIncomingInstancesTheSame())
			{
				var (instancesToRemove, instancesToAdd) = _instancesData.GetIncomingDiff();
				foreach (var instanceData in instancesToRemove)
					DestroyInstanceInternal(instanceData.ID);

				foreach (var instanceData in instancesToAdd)
				{
					_networkIdEnumerator.MoveTo(instanceData.PrecedingNetworkIdEnumeratorValue);
					CreateInstanceInternal(instanceData.ID, instanceData.InstanceType);
				}

				_instancesData.ApplyIncomingInstances();
			}

			_networkIdEnumerator.MoveTo(_currentNetworkId.Value);
		}

		public void OnPreStateSerialize()
		{
			_currentNetworkId.Value = _networkIdEnumerator.GetCurrent();
		}

		private void OnEnable()
		{
			AvailableFactories.Add(this);
		}

		private void OnDisable()
		{
			AvailableFactories.Remove(this);
		}

		private class CreatedInstanceWrapper
		{
			public GameObject GameObject { get; set; }
			public List<int>  NetworkIds { get; set; }
		}
	}
}
