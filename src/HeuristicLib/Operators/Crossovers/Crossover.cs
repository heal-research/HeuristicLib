using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies combining parent groups into offspring when declaring an operator slot.</summary>
public sealed class Crossover : IItemOperation<Crossover>
{
    static bool IOperation<Crossover>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Crossover>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Crossover>.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate, TSearchSpace, TProblem>(children);

    static bool IOperation<Crossover>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<TCandidate> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Cross(parents, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TCandidate>>(new(Executor, parents, random, searchSpace, problem));
    }

    private sealed class RoutedExecutor<TCandidate, TSearchSpace, TProblem>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>>(children), ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Children.Route(parents, random, (child, batch) => child.Cross(batch, random, searchSpace, problem));
    }
}
