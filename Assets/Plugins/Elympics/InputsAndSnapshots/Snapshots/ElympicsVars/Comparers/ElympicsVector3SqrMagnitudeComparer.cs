using UnityEngine;

namespace Elympics
{
	public class ElympicsVector3SqrMagnitudeComparer : IElympicsVarEqualityComparer<Vector3>
	{
		private readonly float _tolerance;

		public ElympicsVector3SqrMagnitudeComparer(float tolerance = 0.01f) => _tolerance = tolerance;

		public bool Equals(Vector3 v1, Vector3 v2) => (v1 - v2).sqrMagnitude <= _tolerance;
	}
}
