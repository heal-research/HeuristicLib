using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies refining candidates when declaring an operator slot.</summary>
public sealed class Refinement : IItemOperation<Refinement>
{
    static bool IOperation<Refinement>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IRefinementExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Refinement>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Refinement>.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate, TSearchSpace, TProblem>(children);

    static bool IOperation<Refinement>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IRefinementExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Refine(candidates, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TCandidate>>(new(Executor, candidates, random, searchSpace, problem));
    }

    private sealed class RoutedExecutor<TCandidate, TSearchSpace, TProblem>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>>(children), IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Children.Route(candidates, random, (child, batch) => child.Refine(batch, random, searchSpace, problem));
    }
}
