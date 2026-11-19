using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies selecting candidates from a population when declaring an operator slot.</summary>
public sealed class Selection : IOperation<Selection>
{
    static bool IOperation<Selection>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is ISelectionExecutor<TCandidate>;

    static IExecutor IOperation<Selection>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static bool IOperation<Selection>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(ISelectionExecutor<TCandidate> executor,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> population, int count, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<EvaluatedCandidate<TCandidate>>>
    {
        private readonly ISelectionExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ISelectionExecutor<TCandidate>?)executor ?? this.executor).Select(population, count, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<EvaluatedCandidate<TCandidate>> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ISelectionExecutor<TCandidate>, TWrapper>(executor, wrapper), ISelectionExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, int count, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<EvaluatedCandidate<TCandidate>>>(
                new(Executor, population, count, random));
    }
}
