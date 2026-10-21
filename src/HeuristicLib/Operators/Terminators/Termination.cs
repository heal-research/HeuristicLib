using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies deciding whether a search should stop when declaring an operator slot.</summary>
public sealed class Termination : IOperationDescriptor<Termination>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>
        : OperationExecutorAdapter<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateWrappedExecutor<TWrapper>(
            ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
            TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<bool>, ITerminationArguments
        {
            private readonly ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public ISearchState State => state;
            public bool Proceed() => executor.IsTerminalState(state, random, searchSpace, problem);
            public bool ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>?)executor ?? this.executor).IsTerminalState(state, random ?? this.random, searchSpace, problem);
        }

        private sealed class WrappedExecutor<TWrapper>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, TWrapper wrapper)
            : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, bool>(new Invocation(executor, state, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}

/// <summary>The arguments of one termination call, which its observations read.</summary>
internal interface ITerminationArguments
{
    ISearchState State { get; }
}
