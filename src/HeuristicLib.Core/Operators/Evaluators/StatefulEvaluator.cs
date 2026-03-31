using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record StatefulEvaluator<TGenotype, TSearchSpace, TProblem, TState>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulEvaluatorInstance(this, CreateInitialState());

  private sealed class StatefulEvaluatorInstance(StatefulEvaluator<TGenotype, TSearchSpace, TProblem, TState> evaluator, TState state)
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return evaluator.Evaluate(genotypes, state, random, searchSpace, problem);
    }
  }
}

public abstract record StatefulEvaluator<TGenotype, TSearchSpace, TState>
  : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulEvaluatorInstance(this, CreateInitialState());

  private sealed class StatefulEvaluatorInstance(StatefulEvaluator<TGenotype, TSearchSpace, TState> evaluator, TState state)
    : IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return evaluator.Evaluate(genotypes, state, random, searchSpace);
    }
  }
}

public abstract record StatefulEvaluator<TGenotype, TState>
  : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TState state,
    IRandomNumberGenerator random);

  public IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulEvaluatorInstance(this, CreateInitialState());

  private sealed class StatefulEvaluatorInstance(StatefulEvaluator<TGenotype, TState> evaluator, TState state)
    : IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return evaluator.Evaluate(genotypes, state, random);
    }
  }
}
