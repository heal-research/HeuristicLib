using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class RepeatingEvaluator
{
  public static RepeatingEvaluator<TGenotype, TSearchSpace, TProblem> AsRepeatingAggregating<TGenotype, TSearchSpace, TProblem>(
    this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    int repeats,
    Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => new(evaluator, repeats, aggregator);

  public static RepeatedEvaluator<TGenotype, TSearchSpace, TProblem> AsRepeated<TGenotype, TSearchSpace, TProblem>(
    this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    int repeats,
    Func<ObjectiveVector[], ObjectiveVector> aggregator, int maxDegreeOfParallelism = -1)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
    => new(evaluator, repeats, aggregator, maxDegreeOfParallelism);
}

public record RepeatingEvaluator<TGenotype, TSearchSpace, TProblem>
  : DecoratorEvaluator<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly int repeats;
  private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

  public RepeatingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
    : base(evaluator)
  {
    this.repeats = repeats;
    this.aggregator = aggregator;
  }

  protected override NoState CreateInitialState() => NoState.Instance;

  protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, NoState state,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> innerEvaluator, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem)
  {
    var results = innerEvaluator.Evaluate(genotypes, random, searchSpace, problem).ToArray();

    for (var i = 0; i < repeats; i++) {
      var reevaluationResult = innerEvaluator.Evaluate(genotypes, random, searchSpace, problem);
      for (var j = 0; j < results.Length; j++) {
        results[j] = aggregator(results[j], reevaluationResult[j]);
      }
    }

    return results;
  }
}

public record RepeatedEvaluator<TGenotype, TSearchSpace, TProblem>
  : DecoratorEvaluator<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly int repeats;
  private readonly Func<ObjectiveVector[], ObjectiveVector> aggregator;
  private readonly int maxDegreeOfParallelism;

  public RepeatedEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator, int maxDegreeOfParallelism = -1)
    : base(evaluator)
  {
    this.repeats = repeats;
    this.aggregator = aggregator;
    this.maxDegreeOfParallelism = maxDegreeOfParallelism;
  }

  protected override NoState CreateInitialState() => NoState.Instance;

  protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, NoState state,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> innerEvaluator, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem)
  {
    var res = BatchExecution.Parallel(
      repeats,
      r => innerEvaluator.Evaluate(genotypes, r, searchSpace, problem),
      random,
      maxDegreeOfParallelism: maxDegreeOfParallelism);
    return Enumerable.Range(0, genotypes.Count)
                     .Select(i => Enumerable.Range(0, repeats).Select(j => res[j][i]).ToArray())
                     .Select(aggregator)
                     .ToArray();
  }
}
