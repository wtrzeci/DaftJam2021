using System;

namespace Elympics
{
	public class ElympicsFloatToleranceComparer : IElympicsVarEqualityComparer<float>
	{
		private readonly float _tolerance;

		public ElympicsFloatToleranceComparer(float tolerance = 0.01f) => _tolerance = tolerance;

		public bool Equals(float x, float y) => Math.Abs(x - y) <= _tolerance;
	}
}
