using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies mutating candidates when declaring an operator slot.</summary>
public sealed class Mutation : IItemOperation<Mutation>
{
    static bool IOperation<Mutation>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IMutationExecutor<TCandidate>;

    static IExecutor IOperation<Mutation>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Mutation>.Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate>(children);

    static bool IOperation<Mutation>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(IMutationExecutor<TCandidate> executor,
        IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<TCandidate>>
    {
        private readonly IMutationExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMutationExecutor<TCandidate>?)executor ?? this.executor).Mutate(parents, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMutationExecutor<TCandidate>, TWrapper>(executor, wrapper), IMutationExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<TCandidate>>(new(Executor, parents, random));
    }

    private sealed class RoutedExecutor<TCandidate>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IMutationExecutor<TCandidate>>(children), IMutationExecutor<TCandidate>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
            Children.Route(parents, random, (child, batch) => child.Mutate(batch, random));
    }
}
