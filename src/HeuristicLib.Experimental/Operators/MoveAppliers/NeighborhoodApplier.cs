using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

public sealed record NeighborhoodApplier<TCandidate, TSearchSpace, TProblem, TMove>(
    IDirectNeighborhood<TCandidate, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveApplier<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override TCandidate Apply(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem) =>
        neighborhood.Apply(candidate, move, random, searchSpace, problem);
}
