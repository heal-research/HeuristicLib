using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies choosing survivors from a population and its offspring when declaring an operator slot.</summary>
public sealed class Replacement : IOperation<Replacement>
{
    static bool IOperation<Replacement>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IReplacementExecutor<TCandidate>;

    static IExecutor IOperation<Replacement>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static bool IOperation<Replacement>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(IReplacementExecutor<TCandidate> executor,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
        ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        : IInvocation<IReadOnlyList<EvaluatedCandidate<TCandidate>>>
    {
        private readonly IReplacementExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IReplacementExecutor<TCandidate>?)executor ?? this.executor)
                .Replace(previousPopulation, offspringPopulation, objective, count, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<EvaluatedCandidate<TCandidate>> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IReplacementExecutor<TCandidate>, TWrapper>(executor, wrapper), IReplacementExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<EvaluatedCandidate<TCandidate>>>(
                new(Executor, previousPopulation, offspringPopulation, objective, count, random));
    }
}
