using System;
using UnityEngine;

namespace Elympics
{
	public class ElympicsQuaternionAngleComparer : IElympicsVarEqualityComparer<Quaternion>
	{
		private readonly float _toleranceInDegrees;

		public ElympicsQuaternionAngleComparer(float toleranceInDegrees = 1.0f) => _toleranceInDegrees = toleranceInDegrees;

		public bool Equals(Quaternion x, Quaternion y) => Math.Abs(Quaternion.Angle(x, y)) <= _toleranceInDegrees;
	}
}
