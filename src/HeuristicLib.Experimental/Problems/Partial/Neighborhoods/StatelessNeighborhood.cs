using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : INeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public virtual INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract IEnumerable<TMove> Moves(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public abstract bool RandomMove(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem,
        [MaybeNullWhen(false)] out TMove move);

    public abstract TCandidate ApplyMove(
        TCandidate candidate,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
