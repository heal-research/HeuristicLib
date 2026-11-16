using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

/// <summary>A stateless move operator whose executor retains its bound problem.</summary>
public abstract record StatelessMoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveEvaluation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveEvaluationExecutor<TCandidate, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IMoveEvaluationExecutor<TCandidate, TMove> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotSupportedException("A move evaluator requires an objective and a move.");

    private sealed class Executor(StatelessMoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove> configuration, TProblem problem) : IMoveEvaluationExecutor<TCandidate, TMove>
    {
        public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random) => configuration.Evaluate(oldQuality, candidate, move, random, problem.SearchSpace, problem);
        public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random) => configuration.Evaluate(candidate, random, problem.SearchSpace, problem);
    }
}
