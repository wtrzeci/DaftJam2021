using UnityEngine;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Elympics
{
	public class ElympicsQuaternionExactComparer : IElympicsVarEqualityComparer<Quaternion>
	{
		public bool Equals(Quaternion q1, Quaternion q2) => q1.x == q2.x && q1.y == q2.y && q1.z == q2.z && q1.w == q2.w;
	}
}
