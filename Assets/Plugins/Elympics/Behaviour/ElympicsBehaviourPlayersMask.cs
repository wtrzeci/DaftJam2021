using System.Runtime.CompilerServices;

namespace Elympics
{
	internal enum ElympicsBehaviourPlayersMask
	{
		None     = 0,
		Player1  = 1 << 0,
		Player2  = 1 << 1,
		Player3  = 1 << 2,
		Player4  = 1 << 3,
		Player5  = 1 << 4,
		Player6  = 1 << 5,
		Player7  = 1 << 6,
		Player8  = 1 << 7,
		Player9  = 1 << 8,
		Player10 = 1 << 9,
		Player11 = 1 << 10,
		Player12 = 1 << 11,
		Player13 = 1 << 12,
		Player14 = 1 << 13,
		Player15 = 1 << 14,
		Player16 = 1 << 15,
		Player17 = 1 << 16,
		Player18 = 1 << 17,
		Player19 = 1 << 18,
		Player20 = 1 << 19,
		Player21 = 1 << 20,
		Player22 = 1 << 21,
		Player23 = 1 << 22,
		Player24 = 1 << 23,
		Player25 = 1 << 24,
		Player26 = 1 << 25,
		Player27 = 1 << 26,
		Player28 = 1 << 27,
		Player29 = 1 << 28,
		Player30 = 1 << 29,
		Player31 = 1 << 30,
		All      = ~0
	}

	internal static class ElympicsBehaviourPlayersMaskHelper
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetPlayerIdMask(this int playerId) => 1 << playerId;
	}
}
