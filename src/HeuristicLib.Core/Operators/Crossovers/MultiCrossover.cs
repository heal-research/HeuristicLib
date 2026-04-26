using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public abstract partial record MultiCrossover<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> InnerCrossovers { get; }

  protected delegate IReadOnlyList<TGenotype> InnerCross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected MultiCrossover(ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> innerCrossovers)
  {
    InnerCrossovers = innerCrossovers;
  }

  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerCrossovers.Select(instanceRegistry.Resolve).Select(x => (InnerCross)x.Cross).ToArray(), CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TExecutionState executionState,
    IReadOnlyList<InnerCross> innerCrossovers,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(MultiCrossover<TGenotype, TSearchSpace, TProblem, TExecutionState> multiCrossover, IReadOnlyList<InnerCross> innerCrossovers, TExecutionState executionState)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return multiCrossover.Cross(parents, executionState, innerCrossovers, random, searchSpace, problem);
    }
  }
}

public abstract record MultiCrossover<TGenotype, TSearchSpace, TProblem>
  : MultiCrossover<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected MultiCrossover(ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> innerCrossovers)
    : base(innerCrossovers)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, NoState executionState,
    IReadOnlyList<InnerCross> innerCrossovers,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    => Cross(parents, innerCrossovers, random, searchSpace, problem);

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents,
    IReadOnlyList<InnerCross> innerCrossovers,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
