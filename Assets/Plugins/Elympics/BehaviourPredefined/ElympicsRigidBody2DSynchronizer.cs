using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody2D))]
	public class ElympicsRigidBody2DSynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
	{
		[SerializeField] private bool synchronizePosition        = true;
		[SerializeField] private bool synchronizeRotation        = true;
		[SerializeField] private bool synchronizeVelocity        = true;
		[SerializeField] private bool synchronizeAngularVelocity = true;
		[SerializeField] private bool synchronizeDrag            = false;
		[SerializeField] private bool synchronizeAngularDrag     = false;
		[SerializeField] private bool synchronizeInertia         = false;
		[SerializeField] private bool synchronizeMass            = false;
		[SerializeField] private bool synchronizeGravityScale    = false;

		private bool SynchronizeMass => synchronizeMass && !Rigidbody2D.useAutoMass;

		private Rigidbody2D _rigidbody2D;

		public Rigidbody2D Rigidbody2D
		{
			get => _rigidbody2D ?? (_rigidbody2D = GetComponent<Rigidbody2D>());
		}

		private ElympicsVector2 _position;
		private ElympicsFloat   _rotation;
		private ElympicsVector2 _velocity;
		private ElympicsFloat   _angularVelocity;
		private ElympicsFloat   _drag;
		private ElympicsFloat   _angularDrag;
		private ElympicsFloat   _inertia;
		private ElympicsFloat   _mass;
		private ElympicsFloat   _gravityScale;

		public void Initialize()
		{
			_position = new ElympicsVector2(default, synchronizePosition, new ElympicsVector2SqrMagnitudeComparer());
			_rotation = new ElympicsFloat(default, synchronizeRotation, new ElympicsFloatToleranceComparer());
			_velocity = new ElympicsVector2(default, synchronizeVelocity, new ElympicsVector2SqrMagnitudeComparer());
			_angularVelocity = new ElympicsFloat(default, synchronizeAngularVelocity, new ElympicsFloatToleranceComparer());
			_drag = new ElympicsFloat(default, synchronizeDrag, new ElympicsFloatToleranceComparer());
			_angularDrag = new ElympicsFloat(default, synchronizeAngularDrag, new ElympicsFloatToleranceComparer());
			_inertia = new ElympicsFloat(default, synchronizeInertia, new ElympicsFloatToleranceComparer());
			_mass = new ElympicsFloat(default, SynchronizeMass, new ElympicsFloatToleranceComparer());
			_gravityScale = new ElympicsFloat(default, synchronizeGravityScale, new ElympicsFloatToleranceComparer());
		}

		public void OnPostStateDeserialize()
		{
			if (synchronizePosition)
				Rigidbody2D.position = _position;
			if (synchronizeRotation)
				Rigidbody2D.rotation = _rotation;
			if (synchronizeVelocity)
				Rigidbody2D.velocity = _velocity;
			if (synchronizeAngularVelocity)
				Rigidbody2D.angularVelocity = _angularVelocity;
			if (synchronizeDrag)
				Rigidbody2D.drag = _drag;
			if (synchronizeAngularDrag)
				Rigidbody2D.angularDrag = _angularDrag;
			if (synchronizeInertia)
				Rigidbody2D.inertia = _inertia;
			if (SynchronizeMass)
				Rigidbody2D.mass = _mass;
			if (synchronizeGravityScale)
				Rigidbody2D.gravityScale = _gravityScale;
		}

		public void OnPreStateSerialize()
		{
			if (synchronizePosition)
				_position.Value = Rigidbody2D.position;
			if (synchronizeRotation)
				_rotation.Value = Rigidbody2D.rotation;
			if (synchronizeVelocity)
				_velocity.Value = Rigidbody2D.velocity;
			if (synchronizeAngularVelocity)
				_angularVelocity.Value = Rigidbody2D.angularVelocity;
			if (synchronizeDrag)
				_drag.Value = Rigidbody2D.drag;
			if (synchronizeAngularDrag)
				_angularDrag.Value = Rigidbody2D.angularDrag;
			if (synchronizeInertia)
				_inertia.Value = Rigidbody2D.inertia;
			if (SynchronizeMass)
				_mass.Value = Rigidbody2D.mass;
			if (synchronizeGravityScale)
				_gravityScale.Value = Rigidbody2D.gravityScale;
		}
	}
}
