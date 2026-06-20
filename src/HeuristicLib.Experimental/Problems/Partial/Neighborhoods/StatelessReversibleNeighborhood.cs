using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override IReversibleNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract TGenotype RevertMove(
        TGenotype genotype,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
