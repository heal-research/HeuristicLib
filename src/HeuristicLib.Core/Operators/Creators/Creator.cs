using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record Creator<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : ICreator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Create(int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ICreatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new CreatorInstance(this, CreateInitialState());

  private sealed class CreatorInstance : ICreatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly Creator<TGenotype, TSearchSpace, TProblem, TExecutionState> creator;
    private readonly TExecutionState executionState;

    public CreatorInstance(Creator<TGenotype, TSearchSpace, TProblem, TExecutionState> creator, TExecutionState initialState)
    {
      this.creator = creator;
      executionState = initialState;
    }

    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return creator.Create(count, executionState, random, searchSpace, problem);
    }
  }
}

public abstract record Creator<TGenotype, TSearchSpace, TExecutionState>
  : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Create(int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new CreatorInstance(this, CreateInitialState());

  private sealed class CreatorInstance : ICreatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    private readonly Creator<TGenotype, TSearchSpace, TExecutionState> creator;
    private readonly TExecutionState executionState;

    public CreatorInstance(Creator<TGenotype, TSearchSpace, TExecutionState> creator, TExecutionState initialState)
    {
      this.creator = creator;
      executionState = initialState;
    }

    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return creator.Create(count, executionState, random, searchSpace);
    }
  }
}


public abstract record Creator<TGenotype, TExecutionState>
  : ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Create(int count, TExecutionState executionState, IRandomNumberGenerator random);

  public ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new CreatorInstance(this, CreateInitialState());

  private sealed class CreatorInstance : ICreatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    private readonly Creator<TGenotype, TExecutionState> creator;
    private readonly TExecutionState executionState;

    public CreatorInstance(Creator<TGenotype, TExecutionState> creator, TExecutionState initialState)
    {
      this.creator = creator;
      executionState = initialState;
    }

    public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return creator.Create(count, executionState, random);
    }
  }
}
