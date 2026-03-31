using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record DecoratorReplacer<TGenotype, TSearchSpace, TProblem, TState>
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected IReplacer<TGenotype, TSearchSpace, TProblem> InnerReplacer { get; }
  
  protected DecoratorReplacer(IReplacer<TGenotype, TSearchSpace, TProblem> innerReplacer)
  {
    InnerReplacer = innerReplacer;
  }
  
  public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerReplacer), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TState state,
    IReplacerInstance<TGenotype, TSearchSpace, TProblem> innerReplacer, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(DecoratorReplacer<TGenotype, TSearchSpace, TProblem, TState> decorator,
    IReplacerInstance<TGenotype, TSearchSpace, TProblem> innerReplacer, TState state)
    : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return decorator.Replace(previousPopulation, offspringPopulation, objective, count, state, innerReplacer, random, searchSpace, problem);
    }
  }
}
