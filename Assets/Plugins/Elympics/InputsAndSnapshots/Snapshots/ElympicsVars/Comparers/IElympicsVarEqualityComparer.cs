namespace Elympics
{
	public interface IElympicsVarEqualityComparer<in T>
	{
		bool Equals(T x, T y);
	}
}
