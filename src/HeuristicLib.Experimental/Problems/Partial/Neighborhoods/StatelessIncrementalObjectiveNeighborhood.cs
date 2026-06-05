using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessIncrementalObjectiveNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override IIncrementalObjectiveNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract ObjectiveVector? EvaluateIncrement(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
