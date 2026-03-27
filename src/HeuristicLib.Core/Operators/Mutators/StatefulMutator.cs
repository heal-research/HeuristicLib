using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record StatefulMutator<TGenotype, TSearchSpace, TProblem, TState>
  : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  protected abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  public IMutatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulMutatorInstance(this, CreateInitialState(instanceRegistry));
  
  private sealed class StatefulMutatorInstance : IMutatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly StatefulMutator<TGenotype, TSearchSpace, TProblem, TState> mutator;
    private readonly TState state;

    public StatefulMutatorInstance(StatefulMutator<TGenotype, TSearchSpace, TProblem, TState> mutator, TState initialState)
    {
      this.mutator = mutator;
      state = initialState;
    }

    public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return mutator.Mutate(parents, state, random, searchSpace, problem);
    }
  }
}

public abstract record StatefulMutator<TGenotype, TSearchSpace, TState> 
  : IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  protected abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulMutatorInstance(this, CreateInitialState(instanceRegistry));
  
  private sealed class StatefulMutatorInstance : IMutatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    private readonly StatefulMutator<TGenotype, TSearchSpace, TState> mutator;
    private readonly TState state;

    public StatefulMutatorInstance(StatefulMutator<TGenotype, TSearchSpace, TState> mutator, TState initialState)
    {
      this.mutator = mutator;
      state = initialState;
    }

    public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return mutator.Mutate(parents, state, random, searchSpace);
    }
  }
}

public abstract record StatefulMutator<TGenotype, TState> 
  : IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where  TState : class
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  protected abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, TState state,
    IRandomNumberGenerator random);
  
  public IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulMutatorInstance(this, CreateInitialState(instanceRegistry));
  
  private sealed class StatefulMutatorInstance : IMutatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    private readonly StatefulMutator<TGenotype, TState> mutator;
    private readonly TState state;

    public StatefulMutatorInstance(StatefulMutator<TGenotype, TState> mutator, TState initialState)
    {
      this.mutator = mutator;
      state = initialState;
    }

    public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return mutator.Mutate(parents, state, random);
    }
  }
}
