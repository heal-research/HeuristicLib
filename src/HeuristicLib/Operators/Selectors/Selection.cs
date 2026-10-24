using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies selecting candidates from a population when declaring an operator slot.</summary>
public sealed class Selection : IOperation<Selection>
{
    static bool IOperation<Selection>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is ISelectionExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Selection>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static bool IOperation<Selection>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random,
        TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<EvaluatedCandidate<TCandidate>>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ISelectionExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Select(population, objective, count, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<EvaluatedCandidate<TCandidate>> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ISelectionExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), ISelectionExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
            ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<EvaluatedCandidate<TCandidate>>>(
                new(Executor, population, objective, count, random, searchSpace, problem));
    }
}
