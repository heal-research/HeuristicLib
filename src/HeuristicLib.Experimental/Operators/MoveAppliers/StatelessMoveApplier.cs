namespace HEAL.HeuristicLib.Operators.MoveAppliers;

using Execution;
using Problems;
using Random;
using SearchSpaces;

public abstract record StatelessMoveApplier<TGenotype, TSearchSpace, TProblem, TMove>
    : IMoveApplier<TGenotype, TSearchSpace, TProblem, TMove>,
      IMoveApplierInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public virtual IMoveApplierInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract TGenotype Apply(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
