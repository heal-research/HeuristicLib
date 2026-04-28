using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public record ReevaluationInterceptor<T, TE, TP, TR>
  : IInterceptor<T, TE, TP, TR>
  where TR : PopulationState<T>
  where TE : class, ISearchSpace<T>
  where TP : DynamicProblem<T, TE>
{
  private readonly IEvaluator<T, TE, TP> evaluator;
  private readonly TP subscribedProblem;

  public ReevaluationInterceptor(IEvaluator<T, TE, TP> evaluator, TP problem)
  {
    this.evaluator = evaluator;
    subscribedProblem = problem;
  }

  public IInterceptorInstance<T, TE, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.Resolve(evaluator);
    var instance = new Instance(evaluatorInstance);

    // ToDo: maybe we have a memory leak here?
    subscribedProblem.EpochClock.OnEpochChange += (_, _) => instance.RequestReevaluation();

    return instance;
  }

  private sealed class Instance(IEvaluatorInstance<T, TE, TP> evaluator)
    : IInterceptorInstance<T, TE, TP, TR>
  {
    private int requireReevaluation;

    public void RequestReevaluation() => Interlocked.Increment(ref requireReevaluation);
    public bool ConsumeReevaluationRequest() => Interlocked.Exchange(ref requireReevaluation, 0) != 0;

    public TR Transform(TR currentState, TR? previousState, TE searchSpace, TP problem)
    {
      var result = currentState;

      if (!ConsumeReevaluationRequest()) {
        return result;
      }

      var genotypes = result.Population.Genotypes.ToArray();
      var objectiveVectors = evaluator.Evaluate(genotypes, null!, searchSpace, problem); // random is not available in the interceptor contract.
      result = result with { Population = Population.From(genotypes, objectiveVectors) };

      return result;
    }
  }
}
