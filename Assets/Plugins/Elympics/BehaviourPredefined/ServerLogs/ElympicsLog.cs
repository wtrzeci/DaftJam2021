using System.IO;
using UnityEngine;

namespace Elympics
{
	public class ElympicsLog : ElympicsVar<(LogType, string)>
	{
		public ElympicsLog() : base((LogType.Log, string.Empty)) { }

		public override void Deserialize(BinaryReader br) 
			=> Value = DeserializeInternal(br);

		private static (LogType, string) DeserializeInternal(BinaryReader br)
		{
			var type = (LogType)br.ReadInt32();
			var message = br.ReadString();
			return (type, message);
		}

		public override bool Equals(BinaryReader br1, BinaryReader br2) 
			=> DeserializeInternal(br1).Equals(DeserializeInternal(br2));

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write((int)Value.Item1);
			bw.Write(Value.Item2);
		}
	}
}