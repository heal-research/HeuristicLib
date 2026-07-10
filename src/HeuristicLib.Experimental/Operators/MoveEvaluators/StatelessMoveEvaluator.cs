namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

using Execution;
using Optimization;
using Problems;
using Random;
using SearchSpaces;

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
        ObjectiveVector objective,
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotImplementedException();
}
