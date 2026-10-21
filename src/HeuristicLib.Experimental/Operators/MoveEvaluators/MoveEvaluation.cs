using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating a move, or a candidate without one, when declaring an operator slot.</summary>
/// <remarks>
/// Both methods of the executor pass their calls to the same wrapper, each as its own invocation. Each call evaluates
/// one candidate, so it reports one unit of candidate work.
/// </remarks>
public sealed class MoveEvaluation<TMove> : IOperationDescriptor<MoveEvaluation<TMove>>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override bool SupportsCandidateWork => true;

        protected override IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> CreateWrappedExecutor<TWrapper>(
            IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct MoveInvocation(IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
            ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<ObjectiveVector>
        {
            private readonly IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public ObjectiveVector Proceed() => executor.Evaluate(oldQuality, candidate, move, random, searchSpace, problem);
            public ObjectiveVector ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Evaluate(oldQuality, candidate, move, random ?? this.random, searchSpace, problem);
            public int CountCandidateWork(ObjectiveVector result) => 1;
        }

        private readonly struct CandidateInvocation(IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
            TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<ObjectiveVector>
        {
            private readonly IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public ObjectiveVector Proceed() => executor.Evaluate(candidate, random, searchSpace, problem);
            public ObjectiveVector ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Evaluate(candidate, random ?? this.random, searchSpace, problem);
            public int CountCandidateWork(ObjectiveVector result) => 1;
        }

        private sealed class WrappedExecutor<TWrapper>(IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor, TWrapper wrapper)
            : IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<MoveInvocation, ObjectiveVector>(new MoveInvocation(executor, oldQuality, candidate, move, random, searchSpace, problem));

            public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<CandidateInvocation, ObjectiveVector>(new CandidateInvocation(executor, candidate, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}
