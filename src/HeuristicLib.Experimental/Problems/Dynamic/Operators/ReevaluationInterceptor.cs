using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public sealed record ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : PopulationState<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; }
    public TProblem SourceProblem { get; init; }

    public ReevaluationInterceptor(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, TProblem sourceProblem)
    {
        Evaluator = evaluator;
        SourceProblem = sourceProblem;
    }

    public override InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var instance = new Instance(instanceRegistry.Resolve(Evaluator));

        // The subscription lifetime is shared with DynamicCachingEvaluator and requires a common lifecycle design.
        SourceProblem.EpochClock.OnEpochChange += (_, _) => instance.RequestReevaluation();

        return instance;
    }

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator)
        : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        private int requireReevaluation;

        public void RequestReevaluation() => Interlocked.Increment(ref requireReevaluation);

        private bool ConsumeReevaluationRequest() => Interlocked.Exchange(ref requireReevaluation, 0) != 0;

        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            if (!ConsumeReevaluationRequest())
            {
                return currentState;
            }

            var candidates = currentState.Population.Candidates.ToArray();
            var objectiveVectors = evaluator.Evaluate(candidates, random, searchSpace, problem);
            return currentState with { Population = Population.From(candidates, objectiveVectors) };
        }
    }
}

public static class ReevaluationInterceptor
{
    public static ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, TProblem sourceProblem)
        where TSearchState : PopulationState<TCandidate>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TCandidate, TSearchSpace> =>
        new(evaluator, sourceProblem);
}
