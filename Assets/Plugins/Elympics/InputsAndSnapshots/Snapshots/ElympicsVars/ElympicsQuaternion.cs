using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsQuaternion : ElympicsVar<Quaternion>
	{
		private static readonly IElympicsVarEqualityComparer<Quaternion> DefaultComparer = new ElympicsQuaternionExactComparer();
		private                 IElympicsVarEqualityComparer<Quaternion> _comparer;

		public ElympicsQuaternion(Quaternion value = default, bool enableSynchronization = true, IElympicsVarEqualityComparer<Quaternion> comparer = null) : base(value, enableSynchronization)
		{
			_comparer = comparer ?? DefaultComparer;
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(Value.x);
			bw.Write(Value.y);
			bw.Write(Value.z);
			bw.Write(Value.w);
		}

		public override void       Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private static  Quaternion DeserializeInternal(BinaryReader br)       => new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		public override bool       Equals(BinaryReader br1, BinaryReader br2) => _comparer.Equals(DeserializeInternal(br1), DeserializeInternal(br2));
	}
}
