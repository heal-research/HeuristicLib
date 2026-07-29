using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public interface IMoveEvaluatorInstance<in TGenotype, in TSearchSpace, in TProblem, in TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    ObjectiveVector Evaluate(
        ObjectiveVector oldQuality,
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    ObjectiveVector Evaluate(
        TGenotype genotype,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
