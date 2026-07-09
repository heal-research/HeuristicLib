using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record FullFeatureNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>
    : Neighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>,
      IReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
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

    protected abstract ObjectiveVector? EvaluateIncrement(
        TCandidate candidate,
        TMove move,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    protected abstract ObjectiveVector? BoundIncrement(
        TCandidate candidate,
        TMove move,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public override INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => CreateFullFeatureInstance();

    IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> IReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => CreateFullFeatureInstance();

    IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> IIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => CreateFullFeatureInstance();

    IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> IIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => CreateFullFeatureInstance();

    private Instance CreateFullFeatureInstance()
        => new(this, CreateInitialState());

    public sealed class Instance(
        FullFeatureNeighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState)
        : IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>,
          IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>,
          IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(
            TCandidate candidate,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.Moves(candidate, executionState, random, searchSpace, problem);

        public bool RandomMove(
            TCandidate candidate,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem,
            [MaybeNullWhen(false)] out TMove move)
            => neighborhood.RandomMove(candidate, executionState, random, searchSpace, problem, out move);

        public TCandidate ApplyMove(
            TCandidate candidate,
            TMove move,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.ApplyMove(candidate, move, executionState, searchSpace, problem);

        public TCandidate RevertMove(
            TCandidate candidate,
            TMove move,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.RevertMove(candidate, move, executionState, searchSpace, problem);

        public ObjectiveVector? EvaluateIncrement(
            TCandidate candidate,
            TMove move,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.EvaluateIncrement(candidate, move, executionState, random, searchSpace, problem);

        public ObjectiveVector? BoundIncrement(
            TCandidate candidate,
            TMove move,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.BoundIncrement(candidate, move, executionState, random, searchSpace, problem);
    }
}
