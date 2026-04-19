using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record WrappingSelector<TGenotype, TSearchSpace, TProblem, TState>
  : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected delegate IReadOnlyList<ISolution<TGenotype>> InnerSelect(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected ISelector<TGenotype, TSearchSpace, TProblem> InnerSelector { get; }

  protected WrappingSelector(ISelector<TGenotype, TSearchSpace, TProblem> innerSelector)
  {
    InnerSelector = innerSelector;
  }

  public ISelectorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerSelector).Select, CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TState state, InnerSelect innerSelect,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingSelector<TGenotype, TSearchSpace, TProblem, TState> wrappingSelector,
    InnerSelect innerSelect, TState state)
    : ISelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingSelector.Select(population, objective, count, state, innerSelect, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingSelector<TGenotype, TSearchSpace, TProblem>
  : WrappingSelector<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingSelector(ISelector<TGenotype, TSearchSpace, TProblem> innerSelector)
    : base(innerSelector)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, NoState state, InnerSelect innerSelect,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    => Select(population, objective, count, innerSelect, random, searchSpace, problem);

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, InnerSelect innerSelect,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
