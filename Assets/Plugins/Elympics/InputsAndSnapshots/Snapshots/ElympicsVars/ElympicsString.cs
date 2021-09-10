using System;
using System.IO;

namespace Elympics
{
	[Serializable]
	public class ElympicsString : ElympicsVar<string>
	{
		public ElympicsString() : this(string.Empty)
		{
		}

		public ElympicsString(string value, bool enableSynchronization = true) : base(value, enableSynchronization)
		{
		}

		public override void   Serialize(BinaryWriter bw)                 => bw.Write(Value);
		public override void   Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private static  string DeserializeInternal(BinaryReader br)       => br.ReadString();
		public override bool   Equals(BinaryReader br1, BinaryReader br2) => DeserializeInternal(br1).Equals(DeserializeInternal(br2));
	}
}
