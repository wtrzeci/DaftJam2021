using System;
using System.IO;

namespace Elympics
{
	[Serializable]
	public class ElympicsInt : ElympicsVar<int>
	{
		public ElympicsInt(int value = default, bool enableSynchronization = true) : base(value, enableSynchronization)
		{
		}

		public override void Serialize(BinaryWriter bw)                 => bw.Write(Value);
		public override void Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private         int  DeserializeInternal(BinaryReader br)       => br.ReadInt32();
		public override bool Equals(BinaryReader br1, BinaryReader br2) => DeserializeInternal(br1).Equals(DeserializeInternal(br2));
	}
}
