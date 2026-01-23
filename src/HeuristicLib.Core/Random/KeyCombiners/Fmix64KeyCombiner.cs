namespace HEAL.HeuristicLib.Random.KeyCombiners;

public class Fmix64KeyCombiner : IKeyCombiner
{
  public ulong Combine(ulong parentKey, ulong childKey)
  {
    unchecked {
      var x = parentKey ^ childKey;

      x ^= x >> 33;
      x *= 0xFF51AFD7ED558CCDUL;
      x ^= x >> 33;
      x *= 0xC4CEB9FE1A85EC53UL;
      x ^= x >> 33;

      return x;
    }
  }
}
