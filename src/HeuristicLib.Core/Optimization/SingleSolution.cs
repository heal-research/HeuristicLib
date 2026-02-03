using System.Collections;

namespace HEAL.HeuristicLib.Optimization;

public record SingleISolution<TGenotype>(ISolution<TGenotype> Solution) : IISolutionLayout<TGenotype>
{
  public IEnumerator<ISolution<TGenotype>> GetEnumerator()
  {
    yield return Solution;
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
