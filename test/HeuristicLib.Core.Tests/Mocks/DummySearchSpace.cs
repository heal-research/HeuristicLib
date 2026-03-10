using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Mocks;

public sealed class DummySearchSpace<T> : ISearchSpace<T>
{
  public static readonly DummySearchSpace<T> Instance = new();
  private DummySearchSpace() { }
  public bool Contains(T genotype) => true;
}
