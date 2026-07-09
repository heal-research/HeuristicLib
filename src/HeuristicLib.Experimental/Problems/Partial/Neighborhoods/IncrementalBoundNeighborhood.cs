using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record IncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>
    : Neighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>,
      IIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutionState : class
{
    protected abstract ObjectiveVector? BoundIncrement(
        TCandidate candidate,
        TMove move,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public override IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => new IncrementalBoundNeighborhoodInstance(this, CreateInitialState());

    private sealed class IncrementalBoundNeighborhoodInstance(
        IncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState)
        : IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.Moves(candidate, executionState, random, searchSpace, problem);

        public bool RandomMove(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem, [MaybeNullWhen(false)] out TMove move)
            => neighborhood.RandomMove(candidate, executionState, random, searchSpace, problem, out move);

        public TCandidate ApplyMove(TCandidate candidate, TMove move, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.ApplyMove(candidate, move, executionState, searchSpace, problem);

        public ObjectiveVector? BoundIncrement(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.BoundIncrement(candidate, move, executionState, random, searchSpace, problem);
    }
}
