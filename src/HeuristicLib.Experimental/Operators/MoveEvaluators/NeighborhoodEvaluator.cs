namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

using Neighborhoods;
using Optimization;
using Problems;
using Random;
using SearchSpaces;

public sealed record NeighborhoodEvaluator<TGenotype, TSearchSpace, TProblem, TMove>(
    Neighborhood<TGenotype, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override ObjectiveVector Evaluate(
        ObjectiveVector objective,
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
        => neighborhood.Evaluate(genotype, move, random, searchSpace, problem);
}
