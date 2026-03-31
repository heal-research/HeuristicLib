using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public abstract partial record CompositeCrossover<TGenotype, TSearchSpace, TProblem, TState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> InnerCrossovers { get; }
  
  protected CompositeCrossover(ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> innerCrossovers)
  {
    InnerCrossovers = innerCrossovers;
  }
  
  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerCrossovers.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TState state,
    IReadOnlyList<ICrossoverInstance<TGenotype, TSearchSpace, TProblem>> innerCrossovers,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(CompositeCrossover<TGenotype, TSearchSpace, TProblem, TState> composite, IReadOnlyList<ICrossoverInstance<TGenotype, TSearchSpace, TProblem>> innerCrossovers, TState state)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return composite.Cross(parents, state, innerCrossovers, random, searchSpace, problem);
    }
  }
}
