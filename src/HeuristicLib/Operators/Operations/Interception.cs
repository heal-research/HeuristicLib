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

        protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateSuccessCallbackExecutor<TCallback>(
            IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallbacks callbacks)
            : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState,
                IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    TRunSearchState result;
                    int work;
                    try
                    {
                        result = child.Transform(currentState, previousState, random, searchSpace, problem);
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

        protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreatePolicyExecutor(ImmutableArray<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>> children, SynchronousPolicy policy) : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        {
            public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState,
                IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (currentState, previousState, random, searchSpace, problem), random, static (child, args) =>
                {
                    var result = child.Transform(args.currentState, args.previousState, args.random, args.searchSpace, args.problem);
                    return new OperationCallResult<TRunSearchState>(result, 0);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, TCallback callback)
            : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> where TCallback : struct, IOperationSuccessCallback
        {
            public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState,
                IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Transform(currentState, previousState, random, searchSpace, problem);
                callback.OnSucceeded(0);
                return result;
            }
        }
    }
}
