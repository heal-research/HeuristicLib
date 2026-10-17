using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies combining parent groups into offspring when declaring an operator slot.</summary>
public sealed class Crossover : IOperationDescriptor<Crossover>
{
    public static OperationExecutorAdapter<Crossover, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Crossover, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreateSuccessCallbackExecutor<TCallback>(
            ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks)
            : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    IReadOnlyList<TCandidate> result;
                    int work;
                    try
                    {
                        result = child.Cross(parents, random, searchSpace, problem);
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

        protected override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreatePolicyExecutor(ImmutableArray<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (parents, random, searchSpace, problem), random, static (child, args) =>
                {
                    var result = child.Cross(args.parents, args.random, args.searchSpace, args.problem);
                    return new OperationCallResult<IReadOnlyList<TCandidate>>(result, result.Count);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback)
            : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> where TCallback : struct, IOperationSuccessCallback
        {
            public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Cross(parents, random, searchSpace, problem);
                callback.OnSucceeded(result.Count);
                return result;
            }
        }
    }
}
