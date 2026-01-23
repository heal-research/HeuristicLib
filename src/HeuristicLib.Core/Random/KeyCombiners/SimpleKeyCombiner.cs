namespace HEAL.HeuristicLib.Random.KeyCombiners;

public sealed class SimpleKeyCombiner : IKeyCombiner
{
  public ulong Combine(ulong parentKey, ulong childKey)
  {
    unchecked {
      return parentKey ^ (childKey * 0x9E3779B97F4A7C15UL);
    }
  }
}
