namespace HEAL.HeuristicLib.Random;

public interface IKeyCombiner {
  ulong Combine(ulong parentKey, ulong childKey);
}
