using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
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
        public override bool SupportsCandidateWork => false;

        protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateWrappedExecutor<TWrapper>(
            ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
            TSearchState state, TSearchSpace searchSpace, TProblem problem) : IInvocation<bool>
        {
            public bool Proceed() => executor.IsTerminalState(state, searchSpace, problem);
            public int CountCandidateWork(bool result) => throw new NotSupportedException();
        }

        private sealed class WrappedExecutor<TWrapper>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, TWrapper wrapper)
            : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, bool>(new Invocation(executor, state, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
        }
    }
}
