using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface INeighborhood<TCandidate, in TSearchSpace, in TProblem, TMove>
    : IOperator<INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public interface INeighborhoodInstance<TCandidate, in TSearchSpace, in TProblem, TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IEnumerable<TMove> Moves(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    bool RandomMove(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem,
        [MaybeNullWhen(false)] out TMove move);

    TCandidate ApplyMove(
        TCandidate candidate,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
