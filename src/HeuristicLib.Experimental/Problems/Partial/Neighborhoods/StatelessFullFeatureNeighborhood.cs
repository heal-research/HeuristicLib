using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessFullFeatureNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override INeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    IReversibleNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> IReversibleNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    IIncrementalObjectiveNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> IIncrementalObjectiveNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    IIncrementalBoundNeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> IIncrementalBoundNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract TGenotype RevertMove(
        TGenotype genotype,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);

    public abstract ObjectiveVector? EvaluateIncrement(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public abstract ObjectiveVector? BoundIncrement(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
