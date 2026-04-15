using System.Collections;
using Generator.Equals;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Optimization;

public static class Population
{
  public static Population<TGenotype> From<TGenotype>(IEnumerable<TGenotype> genotypes, IEnumerable<ObjectiveVector> fitnesses) => new([.. genotypes.Zip(fitnesses, Solution.From)]);

  public static Population<TGenotype> From<TGenotype>(IEnumerable<ISolution<TGenotype>> solutions) => new([.. solutions]);
}

[Equatable]
public partial record Population<TGenotype> : IISolutionLayout<TGenotype>
{
  [OrderedEquality]
  public ImmutableArray<ISolution<TGenotype>> Solutions { get; init; }

  public IEnumerable<TGenotype> Genotypes => Solutions.Select(x => x.Genotype);

  public Population(params ImmutableArray<ISolution<TGenotype>> Solutions)
  {
    this.Solutions = Solutions;
  }

  public IEnumerator<ISolution<TGenotype>> GetEnumerator() => Solutions.AsReadOnly().GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class EvaluatorExtensions
{
  public static Population<TGenotype> EvaluatePopulation<TGenotype, TSearchSpace, TProblem>(this IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return Population.From(genotypes, evaluator.Evaluate(genotypes, random, searchSpace, problem));
  }
}
