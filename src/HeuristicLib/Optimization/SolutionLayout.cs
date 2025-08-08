using System.Collections;

namespace HEAL.HeuristicLib.Optimization;

public interface ISolutionLayout<TGenotype> : IEnumerable<Solution<TGenotype>> {
  
}

public record class SingleSolution<TGenotype> : ISolutionLayout<TGenotype> {
  public Solution<TGenotype> Solution { get; init; }
  
  public SingleSolution(Solution<TGenotype> solution) {
    Solution = solution;
  }

  public IEnumerator<Solution<TGenotype>> GetEnumerator() {
    yield return Solution;
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public record class Population<TGenotype> : ISolutionLayout<TGenotype> {
  public ImmutableList<Solution<TGenotype>> Solutions { get; init; }
  
  public Population(ImmutableList<Solution<TGenotype>> solutions) {
    Solutions = solutions;
  }

  public IEnumerator<Solution<TGenotype>> GetEnumerator() {
    return Solutions.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  
  public static Population<TGenotype> From(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses) 
  {
    if (genotypes.Count != fitnesses.Count) throw new ArgumentException("Genotypes and fitnesses must have the same length.");

    var solutions = Enumerable.Zip(genotypes, fitnesses)
      .Select(x => Solution.From(x.First, x.Second));
    return new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(solutions));
  }
}

public static class Population {
  public static Population<TGenotype> From<TGenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses) 
  {
    if (genotypes.Count != fitnesses.Count) throw new ArgumentException("Genotypes and fitnesses must have the same length.");

    var solutions = Enumerable.Zip(genotypes, fitnesses)
      .Select(x => Solution.From(x.First, x.Second));
    return new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(solutions));
  }
}

public record class IslandPopulation<TGenotype> : ISolutionLayout<TGenotype> {
  public ImmutableList<Population<TGenotype>> Islands { get; init; }
  
  public IslandPopulation(ImmutableList<Population<TGenotype>> islands) {
    Islands = islands;
  }

  public IEnumerator<Solution<TGenotype>> GetEnumerator() {
    foreach (var island in Islands) {
      foreach (var solution in island.Solutions) {
        yield return solution;
      }
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
