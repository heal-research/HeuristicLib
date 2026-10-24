using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies transforming a search state using its previous state when declaring an operator slot.</summary>
/// <remarks>
/// An interceptor returns the state it transforms, so the operation names that state, and an interceptor fits only
/// algorithms over exactly that state.
/// </remarks>
public sealed class Interception<TSearchState> : IOperation<Interception<TSearchState>>
    where TSearchState : class, ISearchState
{
    static bool IOperation<Interception<TSearchState>>.IsExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IExecutor executor) =>
        executor is IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>;

    static IExecutor IOperation<Interception<TSearchState>>.Wrap<TCandidate, TSearchSpace, TProblem, TRunSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
        TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public TSearchState Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>?)executor ?? this.executor)
                .Transform(currentState, previousState, random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, TWrapper>(executor, wrapper), IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, TSearchState>(new(Executor, currentState, previousState, random, searchSpace, problem));
    }
}
