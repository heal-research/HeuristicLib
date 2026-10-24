using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies applying a move to a candidate when declaring an operator slot.</summary>
public sealed class MoveApplication<TMove> : IOperation<MoveApplication<TMove>>
{
    static bool IOperation<MoveApplication<TMove>>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>;

    static IExecutor IOperation<MoveApplication<TMove>>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
        TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<TCandidate>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public TCandidate Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Apply(candidate, move, random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>, TWrapper>(executor, wrapper), IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, TCandidate>(new(Executor, candidate, move, random, searchSpace, problem));
    }
}
