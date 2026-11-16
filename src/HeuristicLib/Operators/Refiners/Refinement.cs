using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies refining candidates when declaring an operator slot.</summary>
public sealed class Refinement : IItemOperation<Refinement>
{
    static bool IOperation<Refinement>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IRefinementExecutor<TCandidate>;

    static IExecutor IOperation<Refinement>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Refinement>.Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate>(children);

    static bool IOperation<Refinement>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(IRefinementExecutor<TCandidate> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<TCandidate>>
    {
        private readonly IRefinementExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IRefinementExecutor<TCandidate>?)executor ?? this.executor).Refine(candidates, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IRefinementExecutor<TCandidate>, TWrapper>(executor, wrapper), IRefinementExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<TCandidate>>(new(Executor, candidates, random));
    }

    private sealed class RoutedExecutor<TCandidate>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IRefinementExecutor<TCandidate>>(children), IRefinementExecutor<TCandidate>
    {
        public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Children.Route(candidates, random, (child, batch) => child.Refine(batch, random));
    }
}
