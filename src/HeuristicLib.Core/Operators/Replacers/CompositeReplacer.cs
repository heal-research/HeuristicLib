using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public abstract partial record CompositeReplacer<TGenotype, TSearchSpace, TProblem, TState>
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<IReplacer<TGenotype, TSearchSpace, TProblem>> InnerReplacers { get; }
  
  protected CompositeReplacer(ImmutableArray<IReplacer<TGenotype, TSearchSpace, TProblem>> innerReplacers)
  {
    InnerReplacers = innerReplacers;
  }
  
  public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerReplacers.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TState state,
    IReadOnlyList<IReplacerInstance<TGenotype, TSearchSpace, TProblem>> innerReplacers,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(CompositeReplacer<TGenotype, TSearchSpace, TProblem, TState> composite,
    IReadOnlyList<IReplacerInstance<TGenotype, TSearchSpace, TProblem>> innerReplacers, TState state)
    : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return composite.Replace(previousPopulation, offspringPopulation, objective, count, state, innerReplacers, random, searchSpace, problem);
    }
  }
}
