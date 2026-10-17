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

        protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateSuccessCallbackExecutor<TCallback>(
            ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallbacks callbacks)
            : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public bool IsTerminalState(TRunSearchState state, TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    bool result;
                    int work;
                    try
                    {
                        result = child.IsTerminalState(state, searchSpace, problem);
                        work = 0;
                    }
                    catch (Exception failure)
                    {
                        notifier.NotifyFailed(failure);
                        throw;
                    }
                    notifier.NotifySucceeded(work);
                    return result;
                }
                finally { notifier.Finish(); }
            }
        }

        protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreatePolicyExecutor(ImmutableArray<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>> children, SynchronousPolicy policy) : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        {
            public bool IsTerminalState(TRunSearchState state, TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (state, searchSpace, problem), null, static (child, args) =>
                {
                    var result = child.IsTerminalState(args.state, args.searchSpace, args.problem);
                    return new OperationCallResult<bool>(result, 0);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallback callback)
            : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> where TCallback : struct, IOperationSuccessCallback
        {
            public bool IsTerminalState(TRunSearchState state, TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.IsTerminalState(state, searchSpace, problem);
                callback.OnSucceeded(0);
                return result;
            }
        }
    }
}
