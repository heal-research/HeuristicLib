using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public abstract partial record CompositeSelector<TGenotype, TSearchSpace, TProblem, TState>
  : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<ISelector<TGenotype, TSearchSpace, TProblem>> InnerSelectors { get; }
  
  protected CompositeSelector(ImmutableArray<ISelector<TGenotype, TSearchSpace, TProblem>> innerSelectors)
  {
    InnerSelectors = innerSelectors;
  }
  
  public ISelectorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerSelectors.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TState state, IReadOnlyList<ISelectorInstance<TGenotype, TSearchSpace, TProblem>> innerSelectors,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(CompositeSelector<TGenotype, TSearchSpace, TProblem, TState> composite,
    IReadOnlyList<ISelectorInstance<TGenotype, TSearchSpace, TProblem>> innerSelectors, TState state)
    : ISelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return composite.Select(population, objective, count, state, innerSelectors, random, searchSpace, problem);
    }
  }
}
