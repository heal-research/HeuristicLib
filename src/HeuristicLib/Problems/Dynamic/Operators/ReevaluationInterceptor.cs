using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public class ReevaluationInterceptor<T, TR, TE, TP> : IInterceptor<T, TR, TE, TP>
  where TR : PopulationIterationResult<T>
  where TE : class, IEncoding<T>
  where TP : DynamicProblem<T, TE>
  where T : class {
  private readonly IInterceptor<T, TR, TE, TP>? inner;
  private readonly IEvaluator<T, TE, TP> evaluator;

  private int requireReevaluation = 0;

  public ReevaluationInterceptor(IInterceptor<T, TR, TE, TP>? inner, IEvaluator<T, TE, TP> evaluator, TP problem) {
    this.inner = inner;
    this.evaluator = evaluator;
    problem.EpochClock.OnEpochChange += (_, _) => Interlocked.Increment(ref requireReevaluation);
  }

  public TR Transform(TR currentIterationResult, TR? previousIterationResult, IRandomNumberGenerator random, TE encoding, TP problem) {
    var r = currentIterationResult;
    if (inner != null)
      r = inner.Transform(currentIterationResult, previousIterationResult, random, encoding, problem);

    bool wasTrue = Interlocked.Exchange(ref requireReevaluation, 0) != 0;

    if (!wasTrue)
      return r;

    var genotypes = r.Population.Genotypes.ToArray();
    var fitnesses = evaluator.Evaluate(genotypes, random, encoding, problem);
    r = r with { Population = Population.From(genotypes, fitnesses) };

    return r;
  }
}
