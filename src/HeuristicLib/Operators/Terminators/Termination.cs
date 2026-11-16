using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies deciding whether a search should stop when declaring an operator slot.</summary>
public sealed class Termination : IOperation<Termination>
{
    static bool IOperation<Termination>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is ITerminationExecutor<TSearchState>;

    static IExecutor IOperation<Termination>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TSearchState, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TSearchState>(ITerminationExecutor<TSearchState> executor,
        TSearchState state, IRandomNumberGenerator random) : IInvocation<bool>
        where TSearchState : class, ISearchState
    {
        private readonly ITerminationExecutor<TSearchState> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public bool Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ITerminationExecutor<TSearchState>?)executor ?? this.executor).IsTerminalState(state, random ?? this.random);
    }

    private sealed class WrappedExecutor<TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ITerminationExecutor<TSearchState>, TWrapper>(executor, wrapper), ITerminationExecutor<TSearchState>
        where TSearchState : class, ISearchState
        where TWrapper : struct, IInvocationWrapper
    {
        public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TSearchState>, bool>(new(Executor, state, random));
    }
}
