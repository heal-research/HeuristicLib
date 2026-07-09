using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record Neighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState>
    : INeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IEnumerable<TMove> Moves(
        TCandidate candidate,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    protected abstract bool RandomMove(
        TCandidate candidate,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem,
        [MaybeNullWhen(false)] out TMove move);

    protected abstract TCandidate ApplyMove(
        TCandidate candidate,
        TMove move,
        TExecutionState executionState,
        TSearchSpace searchSpace,
        TProblem problem);

    public virtual INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => new NeighborhoodInstance(this, CreateInitialState());

    protected sealed class NeighborhoodInstance(
        Neighborhood<TCandidate, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState) : INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
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
    }
}
