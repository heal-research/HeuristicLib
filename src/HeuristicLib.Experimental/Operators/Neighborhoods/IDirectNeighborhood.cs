using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

/// <summary>
/// A neighborhood's moves and their application, called directly rather than through the move roles.
/// </summary>
/// <remarks>
/// This is an authoring contract, not a configuration, so it still names the search space and problem: they are what
/// the neighborhood is written for, and its methods take them.
/// </remarks>
public interface IDirectNeighborhood<TCandidate, in TSearchSpace, in TProblem, TMove>
{
    IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
