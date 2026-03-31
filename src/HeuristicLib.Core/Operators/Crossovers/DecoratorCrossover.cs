using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record DecoratorCrossover<TGenotype, TSearchSpace, TProblem, TState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected ICrossover<TGenotype, TSearchSpace, TProblem> InnerCrossover { get; }
  
  protected DecoratorCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> innerCrossover)
  {
    InnerCrossover = innerCrossover;
  }
  
  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerCrossover), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TState state,
    ICrossoverInstance<TGenotype, TSearchSpace, TProblem> innerCrossover, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(DecoratorCrossover<TGenotype, TSearchSpace, TProblem, TState> decorator, ICrossoverInstance<TGenotype, TSearchSpace, TProblem> innerCrossover, TState state)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return decorator.Cross(parents, state, innerCrossover, random, searchSpace, problem);
    }
  }
}
