using System;
using System.Runtime.InteropServices;

namespace RocketToTheMoon.Core
{
    // A short summary of one saved blueprint, shown as a row in the blueprints list.
    public struct BlueprintInfo
    {
        public string name;
        public int partCount;
        public int price;
        public bool allUnlocked;
        public DateTime savedAt;
    }

    // A union: all three fields share the same 8 bytes of memory.
    // Writing "ticks" (a 64-bit long) fills "low" and "high" (two 32-bit ints), and the other way around.
    [StructLayout(LayoutKind.Explicit)]
    public struct TimeBits
    {
        [FieldOffset(0)]
        public long ticks;

        [FieldOffset(0)]
        public int low;

        [FieldOffset(4)]
        public int high;
    }
}
