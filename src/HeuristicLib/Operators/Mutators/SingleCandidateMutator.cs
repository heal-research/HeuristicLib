using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record SingleCandidateMutator<TCandidate, TSearchSpace, TProblem>
    : StatelessMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate MutateCandidate(TCandidate parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        BatchExecution.Execute(
            parents,
            (mutator: this, searchSpace, problem),
            static (parent, itemRandom, state) => state.mutator.MutateCandidate(parent, itemRandom, state.searchSpace, state.problem),
            random,
            Concurrency);
}

public abstract record SingleCandidateMutator<TCandidate, TSearchSpace>
    : StatelessMutator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate MutateCandidate(TCandidate parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
        BatchExecution.Execute(
            parents,
            (mutator: this, searchSpace),
            static (parent, itemRandom, state) => state.mutator.MutateCandidate(parent, itemRandom, state.searchSpace),
            random,
            Concurrency);
}

public abstract record SingleCandidateMutator<TCandidate>
    : StatelessMutator<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate MutateCandidate(TCandidate parent, IRandomNumberGenerator random);

    public sealed override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
        BatchExecution.Execute(
            parents,
            this,
            static (parent, itemRandom, mutator) => mutator.MutateCandidate(parent, itemRandom),
            random,
            Concurrency);
}
