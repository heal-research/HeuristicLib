using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public record ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : PopulationState<TCandidate>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    private readonly IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator;
    private readonly TProblem subscribedProblem;

    public ReevaluationInterceptor(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, TProblem problem)
    {
        this.evaluator = evaluator;
        subscribedProblem = problem;
    }

    public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var evaluatorInstance = instanceRegistry.Resolve(evaluator);
        var instance = new Instance(evaluatorInstance);

        // ToDo: maybe we have a memory leak here?
        subscribedProblem.EpochClock.OnEpochChange += (_, _) => instance.RequestReevaluation();

        return instance;
    }

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator)
      : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        private int requireReevaluation;

        public void RequestReevaluation() => Interlocked.Increment(ref requireReevaluation);
        public bool ConsumeReevaluationRequest() => Interlocked.Exchange(ref requireReevaluation, 0) != 0;

        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
        {
            var result = currentState;

            if (!ConsumeReevaluationRequest())
            {
                return result;
            }

            var candidates = result.Population.Candidates.ToArray();
            var objectiveVectors = evaluator.Evaluate(candidates, null!, searchSpace, problem); // random is not available in the interceptor contract.
            result = result with { Population = Population.From(candidates, objectiveVectors) };

            return result;
        }
    }
}
