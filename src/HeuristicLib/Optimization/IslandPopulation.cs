using System.Collections;
using HEAL.HeuristicLib.Collections;

namespace HEAL.HeuristicLib.Optimization;

public record IslandPopulation<TGenotype>(ImmutableList<Population<TGenotype>> Islands) : IISolutionLayout<TGenotype> {
  public IEnumerator<ISolution<TGenotype>> GetEnumerator() => Islands.SelectMany(island => island.Solutions).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
