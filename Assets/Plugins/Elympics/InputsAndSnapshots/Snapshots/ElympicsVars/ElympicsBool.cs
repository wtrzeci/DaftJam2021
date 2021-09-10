using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Elympics
{
	[Serializable]
	[DefaultValue(false)]
	public class ElympicsBool : ElympicsVar<bool>
	{
		public ElympicsBool(bool value = default, bool enabledSynchronization = true) : base(value, enabledSynchronization)
		{
		}

		public override void Serialize(BinaryWriter bw)                 => bw.Write(Value);
		public override void Deserialize(BinaryReader br)               => Value = DeserializeInternal(br);
		private static  bool DeserializeInternal(BinaryReader br)       => br.ReadBoolean();
		public override bool Equals(BinaryReader br1, BinaryReader br2) => DeserializeInternal(br1).Equals(DeserializeInternal(br2));
	}
}
