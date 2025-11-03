using System.Collections;
using HEAL.HeuristicLib.Collections;

namespace HEAL.HeuristicLib.Optimization;

public record IslandPopulation<TGenotype>(ImmutableList<Population<TGenotype>> Islands) : ISolutionLayout<TGenotype> {
  public IEnumerator<Solution<TGenotype>> GetEnumerator() => Islands.SelectMany(island => island.Solutions).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
