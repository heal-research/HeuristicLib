using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record WrappingReplacer<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected delegate IReadOnlyList<ISolution<TGenotype>> InnerReplace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected IReplacer<TGenotype, TSearchSpace, TProblem> InnerReplacer { get; }

  protected WrappingReplacer(IReplacer<TGenotype, TSearchSpace, TProblem> innerReplacer)
  {
    InnerReplacer = innerReplacer;
  }

  public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerReplacer).Replace, CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TExecutionState executionState,
    InnerReplace innerReplace, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingReplacer<TGenotype, TSearchSpace, TProblem, TExecutionState> wrappingReplacer,
    InnerReplace innerReplace, TExecutionState executionState)
    : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingReplacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, innerReplace, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingReplacer<TGenotype, TSearchSpace, TProblem>
  : WrappingReplacer<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingReplacer(IReplacer<TGenotype, TSearchSpace, TProblem> innerReplacer)
    : base(innerReplacer)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, NoState executionState,
    InnerReplace innerReplace, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem)
    => Replace(previousPopulation, offspringPopulation, objective, count, innerReplace, random, searchSpace, problem);

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count,
    InnerReplace innerReplace, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);
}
