using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record WrappingCreator<TG, TS, TP, TState>
  : ICreator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  protected delegate IReadOnlyList<TG> InnerCreate(int count, IRandomNumberGenerator random, TS searchSpace, TP problem);

  protected ICreator<TG, TS, TP> InnerCreator { get; }

  protected WrappingCreator(ICreator<TG, TS, TP> innerCreator)
  {
    InnerCreator = innerCreator;
  }

  public ICreatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance(this, instanceRegistry.Resolve(InnerCreator).Create, CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TG> Create(int count, TState state, InnerCreate innerCreate, IRandomNumberGenerator random, TS searchSpace, TP problem);

  private sealed class Instance(WrappingCreator<TG, TS, TP, TState> wrappingCreator, InnerCreate innerCreate, TState state)
   : ICreatorInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Create(int count, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      return wrappingCreator.Create(count, state, innerCreate, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingCreator<TG, TS, TP>
  : WrappingCreator<TG, TS, TP, NoState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  protected WrappingCreator(ICreator<TG, TS, TP> innerCreator)
    : base(innerCreator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TG> Create(int count, NoState state,
    InnerCreate innerCreate, IRandomNumberGenerator random, TS searchSpace, TP problem)
    => Create(count, innerCreate, random, searchSpace, problem);

  protected abstract IReadOnlyList<TG> Create(int count, InnerCreate innerCreate,
    IRandomNumberGenerator random, TS searchSpace, TP problem);
}

