using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating the moves of a candidate when declaring an operator slot.</summary>
public sealed class MoveCreation<TMove> : IOperation<MoveCreation<TMove>>
{
    static bool IOperation<MoveCreation<TMove>>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>;

    static IExecutor IOperation<MoveCreation<TMove>>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
        TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IEnumerable<TMove>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IEnumerable<TMove> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Moves(candidate, random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>, TWrapper>(executor, wrapper), IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IEnumerable<TMove>>(new(Executor, candidate, random, searchSpace, problem));
    }
}
