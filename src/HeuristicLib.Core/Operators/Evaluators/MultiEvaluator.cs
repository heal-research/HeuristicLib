using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

[Equatable]
public abstract partial record MultiEvaluator<TGenotype, TSearchSpace, TProblem, TState>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<IEvaluator<TGenotype, TSearchSpace, TProblem>> InnerEvaluators { get; }

  protected delegate IReadOnlyList<ObjectiveVector> InnerEvaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  protected MultiEvaluator(ImmutableArray<IEvaluator<TGenotype, TSearchSpace, TProblem>> innerEvaluators)
  {
    InnerEvaluators = innerEvaluators;
  }

  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerEvaluators.Select(instanceRegistry.Resolve).Select(x => (InnerEvaluate)x.Evaluate).ToArray(), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TState state,
    IReadOnlyList<InnerEvaluate> innerEvaluators,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(MultiEvaluator<TGenotype, TSearchSpace, TProblem, TState> multiEvaluator,
    IReadOnlyList<InnerEvaluate> innerEvaluators, TState state)
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return multiEvaluator.Evaluate(genotypes, state, innerEvaluators, random, searchSpace, problem);
    }
  }
}

public abstract record MultiEvaluator<TGenotype, TSearchSpace, TProblem>
  : MultiEvaluator<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected MultiEvaluator(ImmutableArray<IEvaluator<TGenotype, TSearchSpace, TProblem>> innerEvaluators)
    : base(innerEvaluators)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, NoState state,
    IReadOnlyList<InnerEvaluate> innerEvaluators,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    => Evaluate(genotypes, innerEvaluators, random, searchSpace, problem);

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes,
    IReadOnlyList<InnerEvaluate> innerEvaluators,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
