using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record SingleCandidateCreator<TCandidate, TSearchSpace, TProblem>
    : StatelessCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate CreateCandidate(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        BatchExecution.Execute(
            count,
            (creator: this, searchSpace, problem),
            static (itemRandom, state) => state.creator.CreateCandidate(itemRandom, state.searchSpace, state.problem),
            random,
            Concurrency);
}

public abstract record SingleCandidateCreator<TCandidate, TSearchSpace>
    : StatelessCreator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate CreateCandidate(IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
        BatchExecution.Execute(
            count,
            (creator: this, searchSpace),
            static (itemRandom, state) => state.creator.CreateCandidate(itemRandom, state.searchSpace),
            random,
            Concurrency);
}

public abstract record SingleCandidateCreator<TCandidate>
    : StatelessCreator<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate CreateCandidate(IRandomNumberGenerator random);

    public sealed override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
        BatchExecution.Execute(
            count,
            this,
            static (itemRandom, creator) => creator.CreateCandidate(itemRandom),
            random,
            Concurrency);
}
