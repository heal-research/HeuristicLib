using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies deciding whether a search should stop when declaring an operator slot.</summary>
public sealed class Termination : IOperation<Termination>
{
    static bool IOperation<Termination>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>;

    static IExecutor IOperation<Termination>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
        TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<bool>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        private readonly ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public bool Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>?)executor ?? this.executor).IsTerminalState(state, random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, TWrapper>(executor, wrapper), ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TWrapper : struct, IInvocationWrapper
    {
        public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem, TSearchState>, bool>(new(Executor, state, random, searchSpace, problem));
    }
}
