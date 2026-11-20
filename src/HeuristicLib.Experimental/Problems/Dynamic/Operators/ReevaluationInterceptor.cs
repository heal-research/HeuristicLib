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

    public static Parameter<ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, IOperator<Evaluation, TCandidate>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static interceptor => interceptor.Evaluator, static (interceptor, value) => interceptor with { Evaluator = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [EvaluatorParameter];

    public ReevaluationInterceptor(IOperator<Evaluation, TCandidate> evaluator)
    {
        Evaluator = evaluator;
    }

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IInterceptionExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var evaluator = scope.Resolve(Evaluator);
        return new Executor(evaluator, scope.Problem);
    }

    private sealed class Executor(IEvaluationExecutor<TCandidate> evaluator, IDynamicProblem<TCandidate, TSearchSpace> problem)
        : InterceptionExecutor<TSearchState>
    {
        private int consumedEpoch = problem.CurrentEpoch;

        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random)
        {
            var epoch = problem.CurrentEpoch;
            if (Interlocked.Exchange(ref consumedEpoch, epoch) == epoch)
            {
                return currentState;
            }

            var candidates = currentState.Population.Candidates.ToArray();
            var objectiveVectors = evaluator.Evaluate(candidates, random);
            return currentState with { Population = Population.From(candidates.ToEvaluated(objectiveVectors)) };
        }
    }
}
