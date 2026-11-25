using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating candidates when declaring an operator slot.</summary>
public sealed class Creation : IItemOperation<Creation>
{
    static bool IOperation<Creation>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is ICreationExecutor<TCandidate>;

    static IExecutor IOperation<Creation>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Creation>.Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate>(children);

    static bool IOperation<Creation>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(ICreationExecutor<TCandidate> executor,
        int count, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<TCandidate>>
    {
        private readonly ICreationExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ICreationExecutor<TCandidate>?)executor ?? this.executor).Create(count, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ICreationExecutor<TCandidate>, TWrapper>(executor, wrapper), ICreationExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<TCandidate>>(new(Executor, count, random));
    }

    private sealed class RoutedExecutor<TCandidate>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<ICreationExecutor<TCandidate>>(children), ICreationExecutor<TCandidate>
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
            Children.Route(count, random, (child, items) => child.Create(items.Count, random));
    }
}
