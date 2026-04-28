using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record DirectEvaluator<TGenotype>
  : StatelessEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    => DirectEvaluator.Evaluate(genotypes, random, problem);
}

public static class DirectEvaluator
{
  public static IReadOnlyList<ObjectiveVector> Evaluate<TGenotype>(
    IReadOnlyList<TGenotype> genotypes,
    IRandomNumberGenerator random,
    IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => problem.Evaluate(genotypes, random);
}

public static class DirectEvaluatorExtensions
{
  public static DirectEvaluator<TGenotype> CreateEvaluator<TGenotype>(this IProblem<TGenotype, ISearchSpace<TGenotype>> algorithm) => new();
}
