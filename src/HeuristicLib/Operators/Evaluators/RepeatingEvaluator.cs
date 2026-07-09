using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class RepeatingEvaluator
{
    public static RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeatingAggregating<TCandidate, TSearchSpace, TProblem>(
      this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
      int repeats,
      Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
      where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(evaluator, repeats, aggregator);

    public static RepeatedEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated<TCandidate, TSearchSpace, TProblem>(
      this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
      int repeats,
      Func<ObjectiveVector[], ObjectiveVector> aggregator, int maxDegreeOfParallelism = -1)
      where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>
      => new(evaluator, repeats, aggregator, maxDegreeOfParallelism);
}

public record RepeatingEvaluator<TCandidate, TSearchSpace, TProblem>
  : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int repeats;
    private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

    public RepeatingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
      : base(evaluator)
    {
        this.repeats = repeats;
        this.aggregator = aggregator;
    }

    protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
      InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem)
    {
        var results = innerEvaluate(candidates, random, searchSpace, problem).ToArray();

        for (var i = 0; i < repeats; i++)
        {
            var reevaluationResult = innerEvaluate(candidates, random, searchSpace, problem);
            for (var j = 0; j < results.Length; j++)
            {
                results[j] = aggregator(results[j], reevaluationResult[j]);
            }
        }

        return results;
    }
}

public record RepeatedEvaluator<TCandidate, TSearchSpace, TProblem>
  : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int repeats;
    private readonly Func<ObjectiveVector[], ObjectiveVector> aggregator;
    private readonly int maxDegreeOfParallelism;

    public RepeatedEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator, int maxDegreeOfParallelism = -1)
      : base(evaluator)
    {
        this.repeats = repeats;
        this.aggregator = aggregator;
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
      InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem)
    {
        var res = BatchExecution.Parallel(
          repeats,
          r => innerEvaluate(candidates, r, searchSpace, problem),
          random,
          maxDegreeOfParallelism: maxDegreeOfParallelism);
        return Enumerable.Range(0, candidates.Count)
                         .Select(i => Enumerable.Range(0, repeats).Select(j => res[j][i]).ToArray())
                         .Select(aggregator)
                         .ToArray();
    }
}
