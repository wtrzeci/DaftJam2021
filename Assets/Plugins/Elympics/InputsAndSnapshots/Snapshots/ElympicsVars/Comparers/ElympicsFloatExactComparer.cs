// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Elympics
{
	public class ElympicsFloatExactComparer : IElympicsVarEqualityComparer<float>
	{
		public bool Equals(float x, float y) => x == y;
	}
}
