using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies choosing survivors from a population and its offspring when declaring an operator slot.</summary>
public sealed class Replacement : IOperation<Replacement>
{
    static bool IOperation<Replacement>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IReplacementExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Replacement>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static bool IOperation<Replacement>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IReplacementExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
        ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        : IInvocation<IReadOnlyList<EvaluatedCandidate<TCandidate>>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IReplacementExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IReplacementExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor)
                .Replace(previousPopulation, offspringPopulation, objective, count, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<EvaluatedCandidate<TCandidate>> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IReplacementExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IReplacementExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count,
            IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<EvaluatedCandidate<TCandidate>>>(
                new(Executor, previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem));
    }
}
