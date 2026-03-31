using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record DecoratorCreator<TG, TS, TP, TState> 
  : ICreator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: is this really an expressive name?
  protected ICreator<TG, TS, TP> InnerCreator { get; }
  
  protected DecoratorCreator(ICreator<TG, TS, TP> innerCreator)
  {
    InnerCreator = innerCreator;
  }

  public ICreatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) 
    => new Instance(this, instanceRegistry.Resolve(InnerCreator), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TG> Create(int count, TState state, ICreatorInstance<TG, TS, TP> innerCreator, IRandomNumberGenerator random, TS searchSpace, TP problem);
  
  private sealed class Instance(DecoratorCreator<TG, TS, TP, TState> decoratorCreator, ICreatorInstance<TG, TS, TP> innerCreatorInstance, TState state)
   : ICreatorInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Create(int count, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      return decoratorCreator.Create(count, state, innerCreatorInstance, random, searchSpace, problem);
    }
  }
}
