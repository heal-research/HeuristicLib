namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

using Optimization;
using Problems;
using Random;
using SearchSpaces;

public interface IMoveEvaluatorInstance<in TGenotype, in TSearchSpace, in TProblem, in TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    /// <summary>
    /// Evaluates new Quality a certain move would bring
    /// </summary>
    /// <param name="genotype"></param>
    /// <param name="move"></param>
    /// <param name="random"></param>
    /// <param name="searchSpace"></param>
    /// <param name="problem"></param>
    /// <returns></returns>
    ObjectiveVector Evaluate(
        ObjectiveVector oldQuality,
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    /// <summary>
    /// Evaluates the current Quality without a move
    /// </summary>
    /// <param name="genotype"></param>
    /// <param name="move"></param>
    /// <param name="random"></param>
    /// <param name="searchSpace"></param>
    /// <param name="problem"></param>
    /// <returns></returns>
    ObjectiveVector Evaluate(
        TGenotype genotype,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
