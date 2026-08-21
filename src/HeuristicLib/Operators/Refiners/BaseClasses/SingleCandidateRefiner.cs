using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public abstract record SingleCandidateRefiner<TCandidate, TSearchSpace, TProblem>
    : StatelessRefiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate RefineCandidate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        BatchExecution.Execute(
            candidates,
            (refiner: this, searchSpace, problem),
            static (candidate, itemRandom, state) => state.refiner.RefineCandidate(candidate, itemRandom, state.searchSpace, state.problem),
            random,
            Concurrency);
}

public abstract record SingleCandidateRefiner<TCandidate, TSearchSpace>
    : StatelessRefiner<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate RefineCandidate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
        BatchExecution.Execute(
            candidates,
            (refiner: this, searchSpace),
            static (candidate, itemRandom, state) => state.refiner.RefineCandidate(candidate, itemRandom, state.searchSpace),
            random,
            Concurrency);
}

public abstract record SingleCandidateRefiner<TCandidate>
    : StatelessRefiner<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate RefineCandidate(TCandidate candidate, IRandomNumberGenerator random);

    public sealed override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(
            candidates,
            this,
            static (candidate, itemRandom, refiner) => refiner.RefineCandidate(candidate, itemRandom),
            random,
            Concurrency);
}
