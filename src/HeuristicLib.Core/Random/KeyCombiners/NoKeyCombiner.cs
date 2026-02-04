namespace HEAL.HeuristicLib.Random.KeyCombiners;

public class NoKeyCombiner : IKeyCombiner
{
  public ulong Combine(ulong parentKey, ulong childKey) => throw new NotImplementedException();
}
