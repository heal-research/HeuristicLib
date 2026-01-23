namespace HEAL.HeuristicLib.Random.RandomEngines;

public sealed class Pcg64Engine : IRandomEngine {
  private ulong state;
  private const ulong Multiplier = 6364136223846793005UL;

  public Pcg64Engine(ulong seed) {
    state = seed + 0x853C49E6748FEA9BUL;
    NextUInt64();
  }

  private ulong NextUInt64() {
    ulong old = state;
    state = unchecked(old * Multiplier + 0xDA3E39CB94B95BDBUL);

    ulong xorshifted = ((old >> 18) ^ old) >> 27;
    int rot = (int)(old >> 59);
    return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
  }

  public int NextInt() => (int)(NextUInt64() & 0x7FFFFFFF);

  public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
}
