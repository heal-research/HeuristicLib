using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating candidates when declaring an operator slot.</summary>
public sealed class Evaluation : IItemOperation<Evaluation>
{
    static bool IOperation<Evaluation>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IEvaluationExecutor<TCandidate>;

    static IExecutor IOperation<Evaluation>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Evaluation>.Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate>(children);

    static bool IOperation<Evaluation>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(IEvaluationExecutor<TCandidate> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<ObjectiveVector>>
    {
        private readonly IEvaluationExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<ObjectiveVector> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IEvaluationExecutor<TCandidate>?)executor ?? this.executor).Evaluate(candidates, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<ObjectiveVector> result) => candidates.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IEvaluationExecutor<TCandidate>, TWrapper>(executor, wrapper), IEvaluationExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<ObjectiveVector>>(new(Executor, candidates, random));
    }

    private sealed class RoutedExecutor<TCandidate>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IEvaluationExecutor<TCandidate>>(children), IEvaluationExecutor<TCandidate>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Children.Route(candidates, random, (child, batch) => child.Evaluate(batch, random));
    }
}
