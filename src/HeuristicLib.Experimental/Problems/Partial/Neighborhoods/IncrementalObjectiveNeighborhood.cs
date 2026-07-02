using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record IncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>
    : Neighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>,
      IIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutionState : class
{
    protected abstract ObjectiveVector? EvaluateIncrement(
        TCandidate candidate,
        TMove move,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public override IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => new IncrementalObjectiveNeighborhoodInstance(this, CreateInitialState());

    private sealed class IncrementalObjectiveNeighborhoodInstance(
        IncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState)
        : IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.Moves(candidate, executionState, random, searchSpace, problem);

        public bool RandomMove(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem, [MaybeNullWhen(false)] out TMove move)
            => neighborhood.RandomMove(candidate, executionState, random, searchSpace, problem, out move);

        public TCandidate ApplyMove(TCandidate candidate, TMove move, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.ApplyMove(candidate, move, executionState, searchSpace, problem);

        public ObjectiveVector? EvaluateIncrement(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.EvaluateIncrement(candidate, move, executionState, random, searchSpace, problem);
    }
}
