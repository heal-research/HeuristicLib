namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

using Problems;
using SearchSpaces;

public interface IMoveEvaluator<in TGenotype, in TSearchSpace, in TProblem, in TMove>
    : IOperator<IMoveEvaluatorInstance<TGenotype, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>;
