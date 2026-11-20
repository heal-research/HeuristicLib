using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies combining parent groups into offspring when declaring an operator slot.</summary>
public sealed class Crossover : IItemOperation<Crossover>
{
    static bool IOperation<Crossover>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is ICrossoverExecutor<TCandidate>;

    static IExecutor IOperation<Crossover>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Crossover>.Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate>(children);

    static bool IOperation<Crossover>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(ICrossoverExecutor<TCandidate> executor,
        IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<TCandidate>>
    {
        private readonly ICrossoverExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ICrossoverExecutor<TCandidate>?)executor ?? this.executor).Cross(parents, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ICrossoverExecutor<TCandidate>, TWrapper>(executor, wrapper), ICrossoverExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<TCandidate>>(new(Executor, parents, random));
    }

    private sealed class RoutedExecutor<TCandidate>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<ICrossoverExecutor<TCandidate>>(children), ICrossoverExecutor<TCandidate>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) =>
            Children.Route(parents, random, (child, batch) => child.Cross(batch, random));
    }
}
