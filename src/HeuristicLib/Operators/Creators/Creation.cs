using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating candidates when declaring an operator slot.</summary>
public sealed class Creation : IItemOperation<Creation>
{
    static bool IOperation<Creation>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is ICreationExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Creation>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Creation>.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate, TSearchSpace, TProblem>(children);

    static bool IOperation<Creation>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(ICreationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly ICreationExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ICreationExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Create(count, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ICreationExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), ICreationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TCandidate>>(new(Executor, count, random, searchSpace, problem));
    }

    private sealed class RoutedExecutor<TCandidate, TSearchSpace, TProblem>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<ICreationExecutor<TCandidate, TSearchSpace, TProblem>>(children), ICreationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Children.Route(count, random, (child, items) => child.Create(items.Count, random, searchSpace, problem));
    }
}
