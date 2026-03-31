using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record DecoratorMutator<TG, TS, TP, TState> 
  : IMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: is this really an expressive name?
  protected IMutator<TG, TS, TP> InnerMutator { get; }
  
  protected DecoratorMutator(IMutator<TG, TS, TP> innerMutator)
  {
    InnerMutator = innerMutator;
  }

  public IMutatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance(this, instanceRegistry.Resolve(InnerMutator), CreateInitialState());
  
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, TState state, IMutatorInstance<TG, TS, TP> innerMutator, IRandomNumberGenerator random, TS searchSpace, TP problem);
  
  private sealed class Instance(DecoratorMutator<TG, TS, TP, TState> decoratorMutator, IMutatorInstance<TG, TS, TP> innerMutator, TState state)
    : IMutatorInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
      => decoratorMutator.Mutate(parents, state, innerMutator, random, searchSpace, problem);
  }
}
