using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

[Equatable]
public abstract partial record CompositeEvaluator<TGenotype, TSearchSpace, TProblem, TState>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<IEvaluator<TGenotype, TSearchSpace, TProblem>> InnerEvaluators { get; }

  protected CompositeEvaluator(ImmutableArray<IEvaluator<TGenotype, TSearchSpace, TProblem>> innerEvaluators)
  {
    InnerEvaluators = innerEvaluators;
  }

  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerEvaluators.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TState state,
    IReadOnlyList<IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>> innerEvaluators,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(CompositeEvaluator<TGenotype, TSearchSpace, TProblem, TState> composite,
    IReadOnlyList<IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>> innerEvaluators, TState state)
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return composite.Evaluate(genotypes, state, innerEvaluators, random, searchSpace, problem);
    }
  }
}
