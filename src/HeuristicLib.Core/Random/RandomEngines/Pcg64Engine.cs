namespace HEAL.HeuristicLib.Random.RandomEngines;

public sealed class Pcg32Engine : IRandomEngine
{
  private ulong state;
  private const ulong Multiplier = 6364136223846793005UL;
  private const ulong Increment = 0xDA3E39CB94B95BDBUL; // must be odd (it is)

  public Pcg32Engine(ulong seed)
  {
    state = seed + 0x853C49E6748FEA9BUL;
    NextUInt32(); // warm up
  }

  private uint NextUInt32()
  {
    ulong old = state;
    state = unchecked(old * Multiplier + Increment);

    uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
    int rot = (int)(old >> 59);
    return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
  }

  public int NextInt() => (int)(NextUInt32() & 0x7FFFFFFF);

  // Make a proper [0,1) double with 53 random bits from TWO 32-bit draws
  public double NextDouble()
  {
    ulong a = NextUInt32();
    ulong b = NextUInt32();
    ulong x = (a << 21) ^ b; // mix to get >=53 bits available
    return (x & ((1UL << 53) - 1)) * (1.0 / (1UL << 53));
  }
}
