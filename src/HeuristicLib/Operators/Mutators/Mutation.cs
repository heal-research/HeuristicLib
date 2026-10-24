using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies mutating candidates when declaring an operator slot.</summary>
public sealed class Mutation : IItemOperation<Mutation>
{
    static bool IOperation<Mutation>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IMutationExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Mutation>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Mutation>.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate, TSearchSpace, TProblem>(children);

    static bool IOperation<Mutation>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMutationExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Mutate(parents, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMutationExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IMutationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TCandidate>>(new(Executor, parents, random, searchSpace, problem));
    }

    private sealed class RoutedExecutor<TCandidate, TSearchSpace, TProblem>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IMutationExecutor<TCandidate, TSearchSpace, TProblem>>(children), IMutationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Children.Route(parents, random, (child, batch) => child.Mutate(batch, random, searchSpace, problem));
    }
}
