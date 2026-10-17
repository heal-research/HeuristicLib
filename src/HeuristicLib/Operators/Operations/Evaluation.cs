using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating candidates when declaring an operator slot.</summary>
public sealed class Evaluation : IOperationDescriptor<Evaluation>
{
    public static OperationExecutorAdapter<Evaluation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Evaluation, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> CreateSuccessCallbackExecutor<TCallback>(
            IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback) => new SuccessCallbackExecutor<TCallback>(child, callback);

        protected override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> CreateCallCallbacksExecutor<TCallbacks, TCallData>(
            IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks) => new CallCallbacksExecutor<TCallbacks, TCallData>(child, callbacks);

        private sealed class CallCallbacksExecutor<TCallbacks, TCallData>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallbacks callbacks)
            : IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var notifier = new OperationCallNotifier<TCallbacks, TCallData>(callbacks);
                try
                {
                    IReadOnlyList<ObjectiveVector> result;
                    int work;
                    try
                    {
                        result = child.Evaluate(candidates, random, searchSpace, problem);
                        work = candidates.Count;
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

        protected override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> CreatePolicyExecutor(ImmutableArray<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) =>
            new PolicyExecutor(children, policy);

        private sealed class PolicyExecutor(ImmutableArray<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>> children, SynchronousPolicy policy) : IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                policy.Execute(children, (candidates, random, searchSpace, problem), random, static (child, args) =>
                {
                    var result = child.Evaluate(args.candidates, args.random, args.searchSpace, args.problem);
                    return new OperationCallResult<IReadOnlyList<ObjectiveVector>>(result, args.candidates.Count);
                }).Value;
        }

        private sealed class SuccessCallbackExecutor<TCallback>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> child, TCallback callback)
            : IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> where TCallback : struct, IOperationSuccessCallback
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Evaluate(candidates, random, searchSpace, problem);
                callback.OnSucceeded(candidates.Count);
                return result;
            }
        }
    }
}
