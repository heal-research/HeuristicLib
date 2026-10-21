using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : PopulationState<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    public IOperator<Evaluation, TCandidate> Evaluator { get; init; }
    public TProblem SourceProblem { get; init; }

    public ReevaluationInterceptor(IOperator<Evaluation, TCandidate> evaluator, TProblem sourceProblem)
    {
        Evaluator = evaluator;
        SourceProblem = sourceProblem;
    }

    protected override ExecutorFactory<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateFactory()
    {
        var state = new ExecutionState(SourceProblem.CurrentEpoch);
        return scope => new Executor(scope.Resolve<TCandidate, TSearchSpace, TProblem>(Evaluator), SourceProblem, state);
    }

    private sealed class ExecutionState(int initialEpoch)
    {
        private int consumedEpoch = initialEpoch;

        public bool ConsumeReevaluationRequest(int epoch) => Interlocked.Exchange(ref consumedEpoch, epoch) != epoch;
    }

    private sealed class Executor(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> evaluator, TProblem sourceProblem, ExecutionState state)
        : InterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            if (!state.ConsumeReevaluationRequest(sourceProblem.CurrentEpoch))
            {
                return currentState;
            }

            var candidates = currentState.Population.Candidates.ToArray();
            var objectiveVectors = evaluator.Evaluate(candidates, random, searchSpace, problem);
            return currentState with { Population = Population.From(candidates.ToEvaluated(objectiveVectors)) };
        }
    }
}

public static class ReevaluationInterceptor
{
    public static ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<Evaluation, TCandidate> evaluator, TProblem sourceProblem)
        where TSearchState : PopulationState<TCandidate>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace> =>
        new(evaluator, sourceProblem);
}
