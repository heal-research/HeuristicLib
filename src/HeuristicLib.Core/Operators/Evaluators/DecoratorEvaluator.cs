using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record DecoratorEvaluator<TGenotype, TSearchSpace, TProblem, TState>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected IEvaluator<TGenotype, TSearchSpace, TProblem> InnerEvaluator { get; }

  protected DecoratorEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> innerEvaluator)
  {
    InnerEvaluator = innerEvaluator;
  }

  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerEvaluator), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TState state,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> innerEvaluator, IRandomNumberGenerator random,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(DecoratorEvaluator<TGenotype, TSearchSpace, TProblem, TState> decorator,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> innerEvaluator, TState state)
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return decorator.Evaluate(genotypes, state, innerEvaluator, random, searchSpace, problem);
    }
  }
}
