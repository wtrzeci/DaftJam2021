using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsVector2 : ElympicsVar<Vector2>
	{
		private readonly        IElympicsVarEqualityComparer<Vector2> _comparer;
		private static readonly IElympicsVarEqualityComparer<Vector2> DefaultComparer = new ElympicsVector2ExactComparer();

		public ElympicsVector2(Vector2 value = default, bool enableSynchronization = true, IElympicsVarEqualityComparer<Vector2> comparer = null) : base(value, enableSynchronization)
		{
			_comparer = comparer ?? DefaultComparer;
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(Value.x);
			bw.Write(Value.y);
		}

		public override void    Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private static  Vector2 DeserializeInternal(BinaryReader br)       => new Vector2(br.ReadSingle(), br.ReadSingle());
		public override bool    Equals(BinaryReader br1, BinaryReader br2) => _comparer.Equals(DeserializeInternal(br1), DeserializeInternal(br2));
	}
}
