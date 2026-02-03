using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public static class ReevaluationInterceptor {
  public static void AttachTo<T, TRes, TE, TP>(IAlgorithmBuilder<T, TE, TP, TRes> proto, TP problem)
    where T : class
    where TRes : PopulationAlgorithmState<T>
    where TE : class, IEncoding<T>
    where TP : DynamicProblem<T, TE> {
    proto.Interceptor = new ReevaluationInterceptor<T, TRes, TE, TP>(
      proto.Interceptor,
      proto.Evaluator,
      problem);
  }
}

public class ReevaluationInterceptor<T, TR, TE, TP> : IInterceptor<T, TR, TE, TP>
  where TR : PopulationAlgorithmState<T>
  where TE : class, IEncoding<T>
  where TP : DynamicProblem<T, TE>
  where T : class {
  private readonly IInterceptor<T, TR, TE, TP>? inner;
  private readonly IEvaluator<T, TE, TP> evaluator;

  private int requireReevaluation;

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
