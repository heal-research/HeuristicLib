using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record ReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>
    : Neighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>,
      IReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutionState : class
{
    protected abstract TCandidate RevertMove(
        TCandidate candidate,
        TMove move,
        TExecutionState executionState,
        TSearchSpace searchSpace,
        TProblem problem);

    public override IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => new ReversibleNeighborhoodInstance(this, CreateInitialState());

    private sealed class ReversibleNeighborhoodInstance(
        ReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState)
        : IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.Moves(candidate, executionState, random, searchSpace, problem);

        public bool RandomMove(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem, [MaybeNullWhen(false)] out TMove move)
            => neighborhood.RandomMove(candidate, executionState, random, searchSpace, problem, out move);

        public TCandidate ApplyMove(TCandidate candidate, TMove move, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.ApplyMove(candidate, move, executionState, searchSpace, problem);

        public TCandidate RevertMove(TCandidate candidate, TMove move, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.RevertMove(candidate, move, executionState, searchSpace, problem);
    }
}
