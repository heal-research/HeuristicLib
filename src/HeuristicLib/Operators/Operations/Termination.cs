using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies deciding whether a search should stop when declaring an operator slot.</summary>
public sealed class Termination : IOperationDescriptor<Termination>
{
    public static OperationExecutorAdapter<Termination, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Termination, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => false;

        protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateInvocationWrapperExecutor(
            ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (TRunSearchState state, TSearchSpace searchSpace, TProblem problem), bool>(
                args => child.IsTerminalState(args.state, args.searchSpace, args.problem), null));

        private sealed class InvocationWrapperExecutor(
            Func<(TRunSearchState state, TSearchSpace searchSpace, TProblem problem), bool> invoke)
            : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        {
            public bool IsTerminalState(TRunSearchState state, TSearchSpace searchSpace, TProblem problem) =>
                invoke((state, searchSpace, problem));
        }

    }
}
