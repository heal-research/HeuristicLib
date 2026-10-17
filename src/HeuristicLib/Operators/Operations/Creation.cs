using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating candidates when declaring an operator slot.</summary>
public sealed class Creation : IOperationDescriptor<Creation>
{
    public static OperationExecutorAdapter<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            ICreationExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override ICreationExecutor<TCandidate, TSearchSpace, TProblem> CreateSuccessCallbackExecutor<TCallback>(
            ICreationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override ICreationExecutor<TCandidate, TSearchSpace, TProblem> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            ICreationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(ICreationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks)
            : ICreationExecutor<TCandidate, TSearchSpace, TProblem> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    IReadOnlyList<TCandidate> result;
                    int work;
                    try
                    {
                        result = child.Create(count, random, searchSpace, problem);
                        work = result.Count;
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

        protected override ICreationExecutor<TCandidate, TSearchSpace, TProblem> CreatePolicyExecutor(ImmutableArray<ICreationExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<ICreationExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : ICreationExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (count, random, searchSpace, problem), random, static (child, args) =>
                {
                    var result = child.Create(args.count, args.random, args.searchSpace, args.problem);
                    return new OperationCallResult<IReadOnlyList<TCandidate>>(result, result.Count);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(ICreationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback)
            : ICreationExecutor<TCandidate, TSearchSpace, TProblem> where TCallback : struct, IOperationSuccessCallback
        {
            public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Create(count, random, searchSpace, problem);
                callback.OnSucceeded(result.Count);
                return result;
            }
        }
    }
}
