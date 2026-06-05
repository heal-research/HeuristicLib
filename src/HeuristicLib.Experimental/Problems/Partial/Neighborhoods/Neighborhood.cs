using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record Neighborhood<TGenotype, TSearchSpace, TProblem, TMove, TExecutionState>
    : INeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IEnumerable<TMove> Moves(
        TGenotype genotype,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    protected abstract bool RandomMove(
        TGenotype genotype,
        TExecutionState executionState,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem,
        [MaybeNullWhen(false)] out TMove move);

    protected abstract TGenotype ApplyMove(
        TGenotype genotype,
        TMove move,
        TExecutionState executionState,
        TSearchSpace searchSpace,
        TProblem problem);

    public virtual INeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => new NeighborhoodInstance(this, CreateInitialState());

    protected sealed class NeighborhoodInstance(
        Neighborhood<TGenotype, TSearchSpace, TProblem, TMove, TExecutionState> neighborhood,
        TExecutionState executionState) : INeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(
            TGenotype genotype,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.Moves(genotype, executionState, random, searchSpace, problem);

        public bool RandomMove(
            TGenotype genotype,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem,
            [MaybeNullWhen(false)] out TMove move)
            => neighborhood.RandomMove(genotype, executionState, random, searchSpace, problem, out move);

        public TGenotype ApplyMove(
            TGenotype genotype,
            TMove move,
            TSearchSpace searchSpace,
            TProblem problem)
            => neighborhood.ApplyMove(genotype, move, executionState, searchSpace, problem);
    }
}
