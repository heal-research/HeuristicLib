using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Mocks;

public static class DummyEvaluator
{
  public static readonly ObjectiveVector DummyObjectives = new(0.0);
}

public record DummyEvaluator<TGenotype, TSearchSpace, TProblem> : SingleSolutionStatelessEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public override ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => DummyEvaluator.DummyObjectives;
}
