using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public abstract partial record MultiSelector<TGenotype, TSearchSpace, TProblem, TState>
  : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<ISelector<TGenotype, TSearchSpace, TProblem>> InnerSelectors { get; }

  protected delegate IReadOnlyList<ISolution<TGenotype>> InnerSelect(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected MultiSelector(ImmutableArray<ISelector<TGenotype, TSearchSpace, TProblem>> innerSelectors)
  {
    InnerSelectors = innerSelectors;
  }

  public ISelectorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerSelectors.Select(instanceRegistry.Resolve).Select(x => (InnerSelect)x.Select).ToArray(), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TState state, IReadOnlyList<InnerSelect> innerSelectors,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(MultiSelector<TGenotype, TSearchSpace, TProblem, TState> multiSelector,
    IReadOnlyList<InnerSelect> innerSelectors, TState state)
    : ISelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return multiSelector.Select(population, objective, count, state, innerSelectors, random, searchSpace, problem);
    }
  }
}

public abstract record MultiSelector<TGenotype, TSearchSpace, TProblem>
  : MultiSelector<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected MultiSelector(ImmutableArray<ISelector<TGenotype, TSearchSpace, TProblem>> innerSelectors)
    : base(innerSelectors)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, NoState state,
    IReadOnlyList<InnerSelect> innerSelectors,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    => Select(population, objective, count, innerSelectors, random, searchSpace, problem);

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, IReadOnlyList<InnerSelect> innerSelectors,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
