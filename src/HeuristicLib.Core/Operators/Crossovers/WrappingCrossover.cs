using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record WrappingCrossover<TGenotype, TSearchSpace, TProblem, TState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected delegate IReadOnlyList<TGenotype> InnerCross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected ICrossover<TGenotype, TSearchSpace, TProblem> InnerCrossover { get; }

  protected WrappingCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> innerCrossover)
  {
    InnerCrossover = innerCrossover;
  }

  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerCrossover).Cross, CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TState state,
    InnerCross innerCross, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingCrossover<TGenotype, TSearchSpace, TProblem, TState> wrappingCrossover, InnerCross innerCross, TState state)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingCrossover.Cross(parents, state, innerCross, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingCrossover<TGenotype, TSearchSpace, TProblem>
  : WrappingCrossover<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> innerCrossover)
    : base(innerCrossover)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, NoState state,
    InnerCross innerCross, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem)
    => Cross(parents, innerCross, random, searchSpace, problem);

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents,
    InnerCross innerCross, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);
}
