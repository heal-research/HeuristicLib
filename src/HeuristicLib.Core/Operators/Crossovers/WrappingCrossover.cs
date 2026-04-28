using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record WrappingCrossover<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected delegate IReadOnlyList<TGenotype> InnerCross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected ICrossover<TGenotype, TSearchSpace, TProblem> InnerCrossover { get; }

  protected WrappingCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> innerCrossover)
  {
    InnerCrossover = innerCrossover;
  }

  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var innerCrossover = instanceRegistry.Resolve(InnerCrossover);
    return new Instance(this, CreateInitialState(), innerCrossover.Cross);
  }

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(
    IReadOnlyList<IParents<TGenotype>> parents,
    TExecutionState executionState,
    InnerCross innerCross,
    IRandomNumberGenerator random,
    TSearchSpace searchSpace,
    TProblem problem);

  private sealed class Instance(
    WrappingCrossover<TGenotype, TSearchSpace, TProblem, TExecutionState> wrappingCrossover,
    TExecutionState executionState,
    InnerCross innerCross)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingCrossover.Cross(parents, executionState, innerCross, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingCrossover<TGenotype, TSearchSpace, TProblem>
  : WrappingCrossover<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> innerCrossover)
    : base(innerCrossover)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, NoState executionState,
    InnerCross innerCross, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem)
    => Cross(parents, innerCross, random, searchSpace, problem);

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents,
    InnerCross innerCross, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);
}
