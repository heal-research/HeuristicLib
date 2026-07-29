using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

public abstract record MoveApplier<TGenotype, TSearchSpace, TProblem, TMove, TState>
    : IMoveApplier<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    private sealed record Instance(
        MoveApplier<TGenotype, TSearchSpace, TProblem, TMove, TState> MoveApplier,
        TState State)
        : IMoveApplierInstance<TGenotype, TSearchSpace, TProblem, TMove>
    {
        public TGenotype Apply(
            TGenotype genotype,
            TMove move,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
            => MoveApplier.Apply(genotype, move, State, searchSpace, problem, random);
    }

    protected abstract TGenotype Apply(
        TGenotype genotype,
        TMove move,
        TState state,
        TSearchSpace searchSpace,
        TProblem problem,
        IRandomNumberGenerator random);

    public virtual IMoveApplierInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => new Instance(this, InitialState());

    protected abstract TState InitialState();
}
