using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

public sealed record NeighborhoodCreator<TCandidate, TSearchSpace, TProblem, TMove>(
    IDirectNeighborhood<TCandidate, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveCreator<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override IEnumerable<TMove> Moves(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem) =>
        neighborhood.Moves(candidate, random, searchSpace, problem);
}
