using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface INeighborhood<TGenotype, in TSearchSpace, in TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    IEnumerable<TMove> Moves(
        TGenotype genotype,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    bool RandomMove(
        TGenotype genotype,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem,
        [MaybeNullWhen(false)] out TMove move);

    TGenotype ApplyMove(
        TGenotype genotype,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
