namespace HEAL.HeuristicLib.Random.RandomEngines;

public sealed class PhiloxEngine : IRandomEngine {
  private ulong counter;
  private readonly ulong key;

  public PhiloxEngine(ulong key) {
    this.key = key;
    counter = 0;
  }

  private ulong PhiloxRound(ulong counter, ulong key) { 
    unchecked {
      ulong hi = Math.BigMul(counter, 0xD2B74407B1CE6E93UL, out ulong lo);
      return hi ^ lo ^ key;
    }
  }

  private ulong NextUInt64() {
    ulong result = PhiloxRound(counter, key);
    counter++;
    return result;
  }

  public int NextInt() => (int)(NextUInt64() & 0x7FFFFFFF);

  public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
}
