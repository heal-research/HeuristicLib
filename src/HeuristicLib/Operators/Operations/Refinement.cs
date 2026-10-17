using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies refining candidates when declaring an operator slot.</summary>
public sealed class Refinement : IOperationDescriptor<Refinement>
{
    public static OperationExecutorAdapter<Refinement, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Refinement, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            IRefinementExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> CreateSuccessCallbackExecutor<TCallback>(
            IRefinementExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            IRefinementExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks)
            : IRefinementExecutor<TCandidate, TSearchSpace, TProblem> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    IReadOnlyList<TCandidate> result;
                    int work;
                    try
                    {
                        result = child.Refine(candidates, random, searchSpace, problem);
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

        protected override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> CreatePolicyExecutor(ImmutableArray<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (candidates, random, searchSpace, problem), random, static (child, args) =>
                {
                    var result = child.Refine(args.candidates, args.random, args.searchSpace, args.problem);
                    return new OperationCallResult<IReadOnlyList<TCandidate>>(result, result.Count);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback)
            : IRefinementExecutor<TCandidate, TSearchSpace, TProblem> where TCallback : struct, IOperationSuccessCallback
        {
            public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Refine(candidates, random, searchSpace, problem);
                callback.OnSucceeded(result.Count);
                return result;
            }
        }
    }
}
