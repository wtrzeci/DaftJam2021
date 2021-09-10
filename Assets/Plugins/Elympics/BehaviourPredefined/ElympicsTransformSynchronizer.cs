using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	public class ElympicsTransformSynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
	{
		[SerializeField] private bool synchronizeLocalPosition = true;
		[SerializeField] private bool synchronizeLocalScale    = true;
		[SerializeField] private bool synchronizeLocalRotation = true;

		private ElympicsVector3    _localPosition;
		private ElympicsVector3    _localScale;
		private ElympicsQuaternion _localRotation;

		public void Initialize()
		{
			_localPosition = new ElympicsVector3(default, synchronizeLocalPosition, new ElympicsVector3SqrMagnitudeComparer());
			_localScale = new ElympicsVector3(default, synchronizeLocalScale, new ElympicsVector3SqrMagnitudeComparer());
			_localRotation = new ElympicsQuaternion(default, synchronizeLocalRotation, new ElympicsQuaternionAngleComparer());
		}

		public void OnPostStateDeserialize()
		{
			var cachedTransform = transform;
			if (synchronizeLocalPosition)
				cachedTransform.localPosition = _localPosition;
			if (synchronizeLocalScale)
				cachedTransform.localScale = _localScale;
			if (synchronizeLocalRotation)
				cachedTransform.localRotation = _localRotation;
		}

		public void OnPreStateSerialize()
		{
			var cachedTransform = transform;
			if (synchronizeLocalPosition)
				_localPosition.Value = cachedTransform.localPosition;
			if (synchronizeLocalScale)
				_localScale.Value = cachedTransform.localScale;
			if (synchronizeLocalRotation)
				_localRotation.Value = cachedTransform.localRotation;
		}
	}
}
