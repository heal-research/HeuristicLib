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

    public static Parameter<ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, IOperator<Evaluation, TCandidate>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static interceptor => interceptor.Evaluator, static (interceptor, value) => interceptor with { Evaluator = value })
        {
            ChildExecution = static _ => ExecutionSignature.For<TSearchSpace, TProblem, ISearchState>()
        };

    public static SettingParameter<ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, TProblem> SourceProblemParameter { get; } =
        new(nameof(SourceProblem), static interceptor => interceptor.SourceProblem, static (interceptor, value) => interceptor with { SourceProblem = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [EvaluatorParameter, SourceProblemParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

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
