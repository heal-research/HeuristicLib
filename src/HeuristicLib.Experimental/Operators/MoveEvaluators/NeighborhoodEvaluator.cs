using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public sealed record NeighborhoodEvaluator<TCandidate, TSearchSpace, TProblem, TMove>(
    Neighborhood<TCandidate, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override ObjectiveVector Evaluate(
        ObjectiveVector oldQuality,
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
        => neighborhood.Evaluate(candidate, move, random, searchSpace, problem);
}
