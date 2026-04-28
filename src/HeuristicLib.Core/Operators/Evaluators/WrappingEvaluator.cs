using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record WrappingEvaluator<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected delegate IReadOnlyList<ObjectiveVector> InnerEvaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected IEvaluator<TGenotype, TSearchSpace, TProblem> InnerEvaluator { get; }

  protected WrappingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> innerEvaluator)
  {
    InnerEvaluator = innerEvaluator;
  }

  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerEvaluator).Evaluate, CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TExecutionState executionState,
    InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingEvaluator<TGenotype, TSearchSpace, TProblem, TExecutionState> wrappingEvaluator,
    InnerEvaluate innerEvaluate, TExecutionState executionState)
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingEvaluator.Evaluate(genotypes, executionState, innerEvaluate, random, searchSpace, problem);
    }
  }
}

public abstract record WrappingEvaluator<TGenotype, TSearchSpace, TProblem>
  : WrappingEvaluator<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> innerEvaluator)
    : base(innerEvaluator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, NoState executionState,
    InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem)
    => Evaluate(genotypes, innerEvaluate, random, searchSpace, problem);

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes,
    InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);
}
