using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child mutator independently for each parent and restores the results to input order.
/// </summary>
/// <remarks>
/// Each selected mutator must return exactly one result for every parent assigned to it.
/// </remarks>
public sealed record ChooseOneMutator<TCandidate> : MultiOperator<Mutation, TCandidate>
{
    /// <summary>
    /// Relative selection weight of each child mutator, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneMutator(IReadOnlyList<IOperator<Mutation, TCandidate>> childMutators)
        : base(childMutators)
    {
    }

    protected override OperatorExecutorFactory CreateOperatorFactory()
    {
        if (ChildOperators.Count == 0)
            throw new InvalidOperationException("At least one mutator must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildOperators.Count)
            throw new InvalidOperationException("Weights must have the same length as mutators.");
        return new Factory(this);
    }

    private sealed class Factory(ChooseOneMutator<TCandidate> source) : OperatorExecutorFactory<Mutation, TCandidate>
    {
        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> childMutators = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor<TRunSearchSpace, TRunProblem>(childMutators, new(childMutators, source.Weights));
        }
    }

    private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> childMutators, WeightedRouter<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> router)
        : MultiMutationExecutor<TCandidate, TSearchSpace, TProblem>(childMutators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            router.Route(parents, random, (mutator, batch) => mutator.Mutate(batch, random, searchSpace, problem));
    }
}

public static class ChooseOneMutator
{
    public static ChooseOneMutator<TCandidate> Create<TCandidate>(params IReadOnlyList<IOperator<Mutation, TCandidate>> childMutators) =>
        new(childMutators);

    public static ChooseOneMutator<TCandidate> Create<TCandidate>(IReadOnlyList<IOperator<Mutation, TCandidate>> childMutators, IReadOnlyList<double> weights) =>
        new(childMutators) { Weights = weights.ToValueArray() };
}

public static class ChooseOneMutatorExtensions
{
    extension<TCandidate>(IOperator<Mutation, TCandidate> mutator)
    {
        public ChooseOneMutator<TCandidate> AppliedAtRate(double mutationRate) =>
            ChooseOneMutator.Create([mutator, new NoChangeMutator<TCandidate>()], WeightedRouter.RateWeights(mutationRate));
    }
}
