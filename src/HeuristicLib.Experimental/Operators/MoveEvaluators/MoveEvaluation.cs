using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating a move, or a candidate without one, when declaring an operator slot.</summary>
/// <remarks>
/// Both methods of the executor pass their calls to the same wrapper, each as its own invocation. Each call evaluates
/// one candidate, so it reports one unit of candidate work.
/// </remarks>
public sealed class MoveEvaluation<TMove> : IOperation<MoveEvaluation<TMove>>
{
    static bool IOperation<MoveEvaluation<TMove>>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IMoveEvaluationExecutor<TCandidate, TMove>;

    static IExecutor IOperation<MoveEvaluation<TMove>>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static bool IOperation<MoveEvaluation<TMove>>.SupportsCandidateWork => true;

    private readonly struct MoveInvocation<TCandidate>(IMoveEvaluationExecutor<TCandidate, TMove> executor,
        ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random) : IInvocation<ObjectiveVector>
    {
        private readonly IMoveEvaluationExecutor<TCandidate, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public ObjectiveVector Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveEvaluationExecutor<TCandidate, TMove>?)executor ?? this.executor).Evaluate(oldQuality, candidate, move, random ?? this.random);

        public int CountCandidateWork(ObjectiveVector result) => 1;
    }

    private readonly struct CandidateInvocation<TCandidate>(IMoveEvaluationExecutor<TCandidate, TMove> executor,
        TCandidate candidate, IRandomNumberGenerator random) : IInvocation<ObjectiveVector>
    {
        private readonly IMoveEvaluationExecutor<TCandidate, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public ObjectiveVector Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveEvaluationExecutor<TCandidate, TMove>?)executor ?? this.executor).Evaluate(candidate, random ?? this.random);

        public int CountCandidateWork(ObjectiveVector result) => 1;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMoveEvaluationExecutor<TCandidate, TMove>, TWrapper>(executor, wrapper), IMoveEvaluationExecutor<TCandidate, TMove>
        where TWrapper : struct, IInvocationWrapper
    {
        public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random) =>
            Wrapper.Invoke<MoveInvocation<TCandidate>, ObjectiveVector>(new(Executor, oldQuality, candidate, move, random));

        public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random) =>
            Wrapper.Invoke<CandidateInvocation<TCandidate>, ObjectiveVector>(new(Executor, candidate, random));
    }
}
