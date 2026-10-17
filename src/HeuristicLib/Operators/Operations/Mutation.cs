using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies mutating candidates when declaring an operator slot.</summary>
public sealed class Mutation : IOperationDescriptor<Mutation>
{
    public static OperationExecutorAdapter<Mutation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Mutation, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            IMutationExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IMutationExecutor<TCandidate, TSearchSpace, TProblem> CreateSuccessCallbackExecutor<TCallback>(
            IMutationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override IMutationExecutor<TCandidate, TSearchSpace, TProblem> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            IMutationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(IMutationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks)
            : IMutationExecutor<TCandidate, TSearchSpace, TProblem> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    IReadOnlyList<TCandidate> result;
                    int work;
                    try
                    {
                        result = child.Mutate(parents, random, searchSpace, problem);
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

        protected override IMutationExecutor<TCandidate, TSearchSpace, TProblem> CreatePolicyExecutor(ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : IMutationExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (parents, random, searchSpace, problem), random, static (child, args) =>
                {
                    var result = child.Mutate(args.parents, args.random, args.searchSpace, args.problem);
                    return new OperationCallResult<IReadOnlyList<TCandidate>>(result, result.Count);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(IMutationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback)
            : IMutationExecutor<TCandidate, TSearchSpace, TProblem> where TCallback : struct, IOperationSuccessCallback
        {
            public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Mutate(parents, random, searchSpace, problem);
                callback.OnSucceeded(result.Count);
                return result;
            }
        }
    }
}
