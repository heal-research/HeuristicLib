using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record StatefulCreator<TGenotype, TSearchSpace, TProblem, TState>
  : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  protected abstract IReadOnlyList<TGenotype> Create(int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulCreatorInstance(this, CreateInitialState(instanceRegistry));

  private sealed class StatefulCreatorInstance : ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly StatefulCreator<TGenotype, TSearchSpace, TProblem, TState> creator;
    private readonly TState state;

    public StatefulCreatorInstance(StatefulCreator<TGenotype, TSearchSpace, TProblem, TState> creator, TState initialState)
    {
      this.creator = creator;
      state = initialState;
    }

    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return creator.Create(count, state, random, searchSpace, problem);
    }
  }
}

public abstract record StatefulCreator<TGenotype, TSearchSpace, TState>
  : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  protected abstract IReadOnlyList<TGenotype> Create(int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulCreatorInstance(this, CreateInitialState(instanceRegistry));

  private sealed class StatefulCreatorInstance : ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    private readonly StatefulCreator<TGenotype, TSearchSpace, TState> creator;
    private readonly TState state;

    public StatefulCreatorInstance(StatefulCreator<TGenotype, TSearchSpace, TState> creator, TState initialState)
    {
      this.creator = creator;
      state = initialState;
    }

    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return creator.Create(count, state, random, searchSpace);
    }
  }
}


public abstract record StatefulCreator<TGenotype, TState>
  : ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  protected abstract IReadOnlyList<TGenotype> Create(int count, TState state, IRandomNumberGenerator random);

  public ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulCreatorInstance(this, CreateInitialState(instanceRegistry));

  private sealed class StatefulCreatorInstance : ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    private readonly StatefulCreator<TGenotype, TState> creator;
    private readonly TState state;

    public StatefulCreatorInstance(StatefulCreator<TGenotype, TState> creator, TState initialState)
    {
      this.creator = creator;
      state = initialState;
    }

    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return creator.Create(count, state, random);
    }
  }
}
