using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsVector3 : ElympicsVar<Vector3>
	{
		private static readonly IElympicsVarEqualityComparer<Vector3> DefaultComparer = new ElympicsVector3ExactComparer();
		private                 IElympicsVarEqualityComparer<Vector3> _comparer;

		public ElympicsVector3(Vector3 value = default, bool enableSynchronization = true, IElympicsVarEqualityComparer<Vector3> comparer = null) : base(value, enableSynchronization)
		{
			_comparer = comparer ?? DefaultComparer;
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(Value.x);
			bw.Write(Value.y);
			bw.Write(Value.z);
		}

		public override void    Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private static  Vector3 DeserializeInternal(BinaryReader br)       => new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		public override bool    Equals(BinaryReader br1, BinaryReader br2) => _comparer.Equals(DeserializeInternal(br1), DeserializeInternal(br2));
	}
}
