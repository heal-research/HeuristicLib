using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

/// <remarks>
/// A move evaluator that carries no mutable execution data is its own execution instance, so it needs no separate instance
/// type. The type arguments are still the search space and problem it is written for, and a run it was not written
/// for is reported when the execution graph is built.
/// </remarks>
public abstract record StatelessMoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove>
    : IMoveEvaluator<TCandidate, TMove>, IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public virtual IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotSupportedException("A move evaluator requires an objective and a move.");

    IMoveEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> IMoveEvaluator<TCandidate, TMove>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not IMoveEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }
}
