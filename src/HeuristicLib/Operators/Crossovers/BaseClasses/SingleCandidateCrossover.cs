using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record SingleCandidateCrossover<TCandidate, TSearchSpace, TProblem>
    : StatelessCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        BatchExecution.Execute(
            parents,
            (crossover: this, searchSpace, problem),
            static (parentGroup, itemRandom, state) => state.crossover.CrossParents(parentGroup, itemRandom, state.searchSpace, state.problem),
            random,
            Concurrency);
}

public abstract record SingleCandidateCrossover<TCandidate, TSearchSpace>
    : StatelessCrossover<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
        BatchExecution.Execute(
            parents,
            (crossover: this, searchSpace),
            static (parentGroup, itemRandom, state) => state.crossover.CrossParents(parentGroup, itemRandom, state.searchSpace),
            random,
            Concurrency);
}

public abstract record SingleCandidateCrossover<TCandidate>
    : StatelessCrossover<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random);

    public sealed override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) =>
        BatchExecution.Execute(
            parents,
            this,
            static (parentGroup, itemRandom, crossover) => crossover.CrossParents(parentGroup, itemRandom),
            random,
            Concurrency);
}
