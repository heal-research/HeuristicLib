using System.Collections;

namespace HEAL.HeuristicLib.Optimization;

public record SingleSolution<TGenotype>(Solution<TGenotype> Solution) : ISolutionLayout<TGenotype> {
  public IEnumerator<Solution<TGenotype>> GetEnumerator() {
    yield return Solution;
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
