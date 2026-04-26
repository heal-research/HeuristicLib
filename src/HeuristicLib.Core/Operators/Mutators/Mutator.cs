using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record Mutator<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IMutatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new MutatorInstance(this, CreateInitialState());

  private sealed class MutatorInstance(Mutator<TGenotype, TSearchSpace, TProblem, TExecutionState> mutator, TExecutionState executionState)
    : IMutatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return mutator.Mutate(parents, executionState, random, searchSpace, problem);
    }
  }
}

public abstract record Mutator<TGenotype, TSearchSpace, TExecutionState>
  : IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new MutatorInstance(this, CreateInitialState());

  private sealed class MutatorInstance : IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    private readonly Mutator<TGenotype, TSearchSpace, TExecutionState> mutator;
    private readonly TExecutionState executionState;

    public MutatorInstance(Mutator<TGenotype, TSearchSpace, TExecutionState> mutator, TExecutionState initialState)
    {
      this.mutator = mutator;
      executionState = initialState;
    }

    public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return mutator.Mutate(parents, executionState, random, searchSpace);
    }
  }
}

public abstract record Mutator<TGenotype, TExecutionState>
  : IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, TExecutionState executionState,
    IRandomNumberGenerator random);

  public IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new MutatorInstance(this, CreateInitialState());

  private sealed class MutatorInstance : IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    private readonly Mutator<TGenotype, TExecutionState> mutator;
    private readonly TExecutionState executionState;

    public MutatorInstance(Mutator<TGenotype, TExecutionState> mutator, TExecutionState initialState)
    {
      this.mutator = mutator;
      executionState = initialState;
    }

    public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return mutator.Mutate(parents, executionState, random);
    }
  }
}
