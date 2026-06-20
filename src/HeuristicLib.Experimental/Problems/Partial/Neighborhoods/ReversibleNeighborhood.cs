using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record ReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove, TExecutionState>
    : Neighborhood<TGenotype, TSearchSpace, TProblem, TMove, TExecutionState>,
      IReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TExecutionState : class
{
    protected abstract TGenotype RevertMove(
        TGenotype genotype,
        TMove move,
        TExecutionState executionState,
        TSearchSpace searchSpace,
        TProblem problem);

    public override IReversibleNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => new ReversibleNeighborhoodInstance(this, CreateInitialState());

    private sealed class ReversibleNeighborhoodInstance(
        ReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState)
        : IReversibleNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.Moves(genotype, executionState, random, searchSpace, problem);

        public bool RandomMove(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem, [MaybeNullWhen(false)] out TMove move)
            => neighborhood.RandomMove(genotype, executionState, random, searchSpace, problem, out move);

        public TGenotype ApplyMove(TGenotype genotype, TMove move, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.ApplyMove(genotype, move, executionState, searchSpace, problem);

        public TGenotype RevertMove(TGenotype genotype, TMove move, TSearchSpace searchSpace, TProblem problem)
            => neighborhood.RevertMove(genotype, move, executionState, searchSpace, problem);
    }
}
