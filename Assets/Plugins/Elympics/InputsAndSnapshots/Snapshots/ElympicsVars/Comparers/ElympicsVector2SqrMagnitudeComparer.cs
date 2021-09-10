using UnityEngine;

namespace Elympics
{
	public class ElympicsVector2SqrMagnitudeComparer : IElympicsVarEqualityComparer<Vector2>
	{
		private readonly float _tolerance;

		public ElympicsVector2SqrMagnitudeComparer(float tolerance = 0.01f) => _tolerance = tolerance;

		public bool Equals(Vector2 v1, Vector2 v2) => (v1 - v2).sqrMagnitude <= _tolerance;
	}
}
