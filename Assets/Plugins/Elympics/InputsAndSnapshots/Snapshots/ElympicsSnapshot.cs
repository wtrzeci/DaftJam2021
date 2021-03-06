using System.Collections.Generic;

namespace Elympics
{
	public class ElympicsSnapshot : ElympicsDataWithTick
	{
		public override long                            Tick { get; set; }
		public          List<KeyValuePair<int, byte[]>> Data { get; set; }
	}
}
