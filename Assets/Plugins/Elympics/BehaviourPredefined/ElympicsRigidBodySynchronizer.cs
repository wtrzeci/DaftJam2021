using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody))]
	public class ElympicsRigidBodySynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
	{
		[SerializeField] private bool synchronizePosition        = true;
		[SerializeField] private bool synchronizeRotation        = true;
		[SerializeField] private bool synchronizeVelocity        = true;
		[SerializeField] private bool synchronizeAngularVelocity = true;
		[SerializeField] private bool synchronizeMass            = false;
		[SerializeField] private bool synchronizeDrag            = false;
		[SerializeField] private bool synchronizeAngularDrag     = false;
		[SerializeField] private bool synchronizeUseGravity      = false;

		private Rigidbody _rigidbody;

		private Rigidbody Rigidbody
		{
			get => _rigidbody ?? (_rigidbody = GetComponent<Rigidbody>());
		}

		private ElympicsVector3    _position;
		private ElympicsQuaternion _rotation;
		private ElympicsVector3    _velocity;
		private ElympicsVector3    _angularVelocity;
		private ElympicsFloat      _mass;
		private ElympicsFloat      _drag;
		private ElympicsFloat      _angularDrag;
		private ElympicsBool       _useGravity;

		public void Initialize()
		{
			_position = new ElympicsVector3(default, synchronizePosition, new ElympicsVector3SqrMagnitudeComparer());
			_rotation = new ElympicsQuaternion(default, synchronizeRotation, new ElympicsQuaternionAngleComparer());
			_velocity = new ElympicsVector3(default, synchronizeVelocity, new ElympicsVector3SqrMagnitudeComparer());
			_angularVelocity = new ElympicsVector3(default, synchronizeAngularVelocity, new ElympicsVector3SqrMagnitudeComparer());
			_mass = new ElympicsFloat(default, synchronizeMass, new ElympicsFloatToleranceComparer());
			_drag = new ElympicsFloat(default, synchronizeDrag, new ElympicsFloatToleranceComparer());
			_angularDrag = new ElympicsFloat(default, synchronizeAngularDrag, new ElympicsFloatToleranceComparer());
			_useGravity = new ElympicsBool(default, synchronizeUseGravity);
		}

		public void OnPostStateDeserialize()
		{
			if (synchronizePosition)
				Rigidbody.position = _position;
			if (synchronizeRotation)
				Rigidbody.rotation = _rotation;
			if (synchronizeVelocity)
				Rigidbody.velocity = _velocity;
			if (synchronizeAngularVelocity)
				Rigidbody.angularVelocity = _angularVelocity;
			if (synchronizeMass)
				Rigidbody.mass = _mass;
			if (synchronizeDrag)
				Rigidbody.drag = _drag;
			if (synchronizeAngularDrag)
				Rigidbody.angularDrag = _angularDrag;
			if (synchronizeUseGravity)
				Rigidbody.useGravity = _useGravity;
		}

		public void OnPreStateSerialize()
		{
			if (synchronizePosition)
				_position.Value = Rigidbody.position;
			if (synchronizeRotation)
				_rotation.Value = Rigidbody.rotation;
			if (synchronizeVelocity)
				_velocity.Value = Rigidbody.velocity;
			if (synchronizeAngularVelocity)
				_angularVelocity.Value = Rigidbody.angularVelocity;
			if (synchronizeMass)
				_mass.Value = Rigidbody.mass;
			if (synchronizeDrag)
				_drag.Value = Rigidbody.drag;
			if (synchronizeAngularDrag)
				_angularDrag.Value = Rigidbody.angularDrag;
			if (synchronizeUseGravity)
				_useGravity.Value = Rigidbody.useGravity;
		}
	}
}
