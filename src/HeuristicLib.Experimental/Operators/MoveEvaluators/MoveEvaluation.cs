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
public sealed class MoveEvaluation<TMove> : IOperation<MoveEvaluation<TMove>>
{
    static bool IOperation<MoveEvaluation<TMove>>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>;

    static IExecutor IOperation<MoveEvaluation<TMove>>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static bool IOperation<MoveEvaluation<TMove>>.SupportsCandidateWork => true;

    private readonly struct MoveInvocation<TCandidate, TSearchSpace, TProblem>(IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
        ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<ObjectiveVector>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public ObjectiveVector Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Evaluate(oldQuality, candidate, move, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(ObjectiveVector result) => 1;
    }

    private readonly struct CandidateInvocation<TCandidate, TSearchSpace, TProblem>(IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
        TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<ObjectiveVector>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public ObjectiveVector Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Evaluate(candidate, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(ObjectiveVector result) => 1;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>, TWrapper>(executor, wrapper), IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<MoveInvocation<TCandidate, TSearchSpace, TProblem>, ObjectiveVector>(new(Executor, oldQuality, candidate, move, random, searchSpace, problem));

        public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<CandidateInvocation<TCandidate, TSearchSpace, TProblem>, ObjectiveVector>(new(Executor, candidate, random, searchSpace, problem));
    }
}
