using System;
using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	[Serializable]
	public class ElympicsFloat : ElympicsVar<float>
	{
		private static readonly IElympicsVarEqualityComparer<float> DefaultComparer = new ElympicsFloatExactComparer();

		private readonly IElympicsVarEqualityComparer<float> _comparer;

		public ElympicsFloat(float value = 0.0f, bool enableSynchronization = true, IElympicsVarEqualityComparer<float> comparer = null) : base(value, enableSynchronization)
		{
			_comparer = comparer ?? DefaultComparer;
		}

		public override void  Serialize(BinaryWriter bw)                 => bw.Write(Value);
		public override void  Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private         float DeserializeInternal(BinaryReader br)       => br.ReadSingle();
		public override bool  Equals(BinaryReader br1, BinaryReader br2) => _comparer.Equals(DeserializeInternal(br1), DeserializeInternal(br2));
	}
}
