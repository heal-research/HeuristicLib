using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessIncrementalBoundNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override IIncrementalBoundNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract ObjectiveVector? BoundIncrement(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
