using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public sealed record NeighborhoodEvaluator<TGenotype, TSearchSpace, TProblem, TMove>(
    Neighborhood<TGenotype, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override ObjectiveVector Evaluate(
        ObjectiveVector oldQuality,
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
        => neighborhood.Evaluate(genotype, move, random, searchSpace, problem);
}
