using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

/// <summary>
/// Applies a child mutator with a dynamic rate that temporarily changes after dynamic problem epoch changes.
/// </summary>
public sealed record DynamicRateMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    private readonly int responseDuration = 1;

    public DynamicRateMutator(IMutator<TCandidate> childMutator)
    {
        ChildMutator = childMutator;
    }

    public IMutator<TCandidate> ChildMutator { get; init; }

    /// <summary>
    /// Gets the ordinary mutation rate. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    public double BaseRate { get; init; } = 0.1;

    /// <summary>
    /// Gets the mutation rate used while the dynamic response is active. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    public double ResponseRate { get; init; } = 1.0;

    /// <summary>
    /// Gets the number of mutation calls after an epoch change that use <see cref="ResponseRate"/>.
    /// </summary>
    public int ResponseDuration
    {
        get => responseDuration;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            responseDuration = value;
        }
    }

    public override ExecutionFactory<MutatorExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory()
    {
        var state = new ExecutionState();
        return scope => new Execution(scope.Resolve<TCandidate, TSearchSpace, TProblem>(ChildMutator), BaseRate, ResponseRate, ResponseDuration, state);
    }

    private sealed class ExecutionState
    {
        public TProblem? SourceProblem { get; set; }
        public int? LastObservedEpoch { get; set; }
        public int RemainingResponseCalls { get; set; }
    }

    private sealed class Execution(
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> childMutator,
        double baseRate,
        double responseRate,
        int responseDuration,
        ExecutionState state)
        : MutatorExecution<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents,
                                                         IRandomNumberGenerator random,
                                                         TSearchSpace searchSpace,
                                                         TProblem problem)
        {
            UpdateResponseState(problem);
            var currentRate = state.RemainingResponseCalls > 0 ? responseRate : baseRate;
            var router = WeightedRouter.AtRate(childMutator, currentRate);
            var result = router.Route(parents, random, (selected, batch) =>
                selected is null ? batch : selected.Mutate(batch, random, searchSpace, problem));

            if (state.RemainingResponseCalls > 0)
                state.RemainingResponseCalls--;

            return result;
        }

        private void UpdateResponseState(TProblem problem)
        {
            if (state.SourceProblem is null)
            {
                state.SourceProblem = problem;
                state.LastObservedEpoch = problem.CurrentEpoch;
                return;
            }

            if (!ReferenceEquals(problem, state.SourceProblem))
                throw new InvalidOperationException("Dynamic rate mutator cannot be reused with a different dynamic problem instance.");

            var currentEpoch = problem.CurrentEpoch;
            if (currentEpoch == state.LastObservedEpoch)
                return;

            state.LastObservedEpoch = currentEpoch;
            state.RemainingResponseCalls = responseDuration;
        }
    }
}

public static class DynamicRateMutatorExtensions
{
    public static DynamicRateMutator<TCandidate, TSearchSpace, TProblem> WithDynamicRate<TCandidate, TSearchSpace, TProblem>(
        this IMutator<TCandidate> mutator,
        DynamicProblem<TProblem, TCandidate, TSearchSpace> problem,
        double baseRate,
        double responseRate,
        int responseDuration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace> =>
        new(mutator)
        {
            BaseRate = baseRate,
            ResponseRate = responseRate,
            ResponseDuration = responseDuration
        };
}
