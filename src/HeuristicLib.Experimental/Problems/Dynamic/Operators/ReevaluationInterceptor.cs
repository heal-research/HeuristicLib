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

    protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor(ResolutionScope scope)
    {
        var evaluator = scope.Resolve<TCandidate, TSearchSpace, TProblem>(Evaluator);
        return new Executor(evaluator, SourceProblem);
    }

    private sealed class Executor(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> evaluator, TProblem sourceProblem)
        : InterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        private int consumedEpoch = sourceProblem.CurrentEpoch;

        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var epoch = sourceProblem.CurrentEpoch;
            if (Interlocked.Exchange(ref consumedEpoch, epoch) == epoch)
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
