using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public abstract record StatelessMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove>
    : IMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove>,
      IMoveEvaluatorInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public virtual IMoveEvaluatorInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract ObjectiveVector Evaluate(
        ObjectiveVector oldQuality,
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotSupportedException("A move evaluator requires an objective and a move.");
}
