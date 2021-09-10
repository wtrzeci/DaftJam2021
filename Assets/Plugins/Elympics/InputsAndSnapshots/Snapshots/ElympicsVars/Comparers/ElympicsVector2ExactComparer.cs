using UnityEngine;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Elympics
{
	public class ElympicsVector2ExactComparer : IElympicsVarEqualityComparer<Vector2>
	{
		public bool Equals(Vector2 v1, Vector2 v2) => v1.x == v2.x && v1.y == v2.y;
	}
}
