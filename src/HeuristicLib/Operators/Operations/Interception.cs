using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies transforming a search state using its previous state when declaring an operator slot.</summary>
public sealed class Interception : IOperationDescriptor<Interception>
{
    public static OperationExecutorAdapter<Interception, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Interception, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => false;

        protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateInvocationWrapperExecutor(
            IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (TRunSearchState currentState, TRunSearchState? previousState, IRandomNumberGenerator random,
                    TSearchSpace searchSpace, TProblem problem), TRunSearchState>(
                args => child.Transform(args.currentState, args.previousState, args.random, args.searchSpace, args.problem), null));

        private sealed class InvocationWrapperExecutor(
            Func<(TRunSearchState currentState, TRunSearchState? previousState, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem), TRunSearchState> invoke)
            : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        {
            public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState,
                IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                invoke((currentState, previousState, random, searchSpace, problem));
        }

    }
}
