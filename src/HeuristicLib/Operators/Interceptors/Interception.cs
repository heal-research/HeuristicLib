using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies transforming a search state using its previous state when declaring an operator slot.</summary>
/// <remarks>
/// An interceptor returns the state it transforms, so the operation names that state, and an interceptor fits only
/// algorithms over exactly that state.
/// </remarks>
public sealed class Interception<TSearchState> : IOperation<Interception<TSearchState>>
    where TSearchState : class, ISearchState
{
    static bool IOperation<Interception<TSearchState>>.IsExecutor<TCandidate, TRunSearchState>(IExecutor executor) =>
        executor is IInterceptionExecutor<TSearchState>;

    static IExecutor IOperation<Interception<TSearchState>>.Wrap<TCandidate, TRunSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TWrapper>(executor, wrapper);

    private readonly struct Invocation(IInterceptionExecutor<TSearchState> executor,
        TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) : IInvocation<TSearchState>
    {
        private readonly IInterceptionExecutor<TSearchState> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public TSearchState Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IInterceptionExecutor<TSearchState>?)executor ?? this.executor)
                .Transform(currentState, previousState, random ?? this.random);
    }

    private sealed class WrappedExecutor<TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IInterceptionExecutor<TSearchState>, TWrapper>(executor, wrapper), IInterceptionExecutor<TSearchState>
        where TWrapper : struct, IInvocationWrapper
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation, TSearchState>(new(Executor, currentState, previousState, random));
    }
}
