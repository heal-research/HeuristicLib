namespace HEAL.HeuristicLib.Random.KeyCombiners;

public sealed class SplitMix64KeyCombiner : IKeyCombiner {
  public ulong Combine(ulong parentKey, ulong childKey) {
    ulong z = parentKey + 0x9E3779B97F4A7C15UL + childKey;
    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
    return z ^ (z >> 31);
  }
}
