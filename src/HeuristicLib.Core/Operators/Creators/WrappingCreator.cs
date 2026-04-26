using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record WrappingCreator<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected delegate IReadOnlyList<TGenotype> InnerCreate(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected ICreator<TGenotype, TSearchSpace, TProblem> InnerCreator { get; }

  protected WrappingCreator(ICreator<TGenotype, TSearchSpace, TProblem> innerCreator)
  {
    InnerCreator = innerCreator;
  }

  public ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var innerCreator = instanceRegistry.Resolve(InnerCreator);
    return new Instance(this, CreateInitialState(), innerCreator.Create);
  }

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Create(
    int count,
    TExecutionState executionState,
    InnerCreate innerCreate,
    IRandomNumberGenerator random,
    TSearchSpace searchSpace,
    TProblem problem);

  private sealed class Instance(
    WrappingCreator<TGenotype, TSearchSpace, TProblem, TExecutionState> wrappingCreator,
    TExecutionState executionState,
    InnerCreate innerCreate)
    : ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingCreator.Create(count, executionState, innerCreate, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingCreator<TGenotype, TSearchSpace, TProblem>
  : WrappingCreator<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingCreator(ICreator<TGenotype, TSearchSpace, TProblem> innerCreator)
    : base(innerCreator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TGenotype> Create(int count, NoState executionState,
    InnerCreate innerCreate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    => Create(count, innerCreate, random, searchSpace, problem);

  protected abstract IReadOnlyList<TGenotype> Create(int count, InnerCreate innerCreate,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
