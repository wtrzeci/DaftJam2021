using UnityEngine;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Elympics
{
	public class ElympicsVector3ExactComparer : IElympicsVarEqualityComparer<Vector3>
	{
		public bool Equals(Vector3 v1, Vector3 v2) => v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;
	}
}
