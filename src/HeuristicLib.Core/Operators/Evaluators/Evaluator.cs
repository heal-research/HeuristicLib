using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record Evaluator<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new EvaluatorInstance(this, CreateInitialState());

  private sealed class EvaluatorInstance(Evaluator<TGenotype, TSearchSpace, TProblem, TExecutionState> evaluator, TExecutionState executionState)
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return evaluator.Evaluate(genotypes, executionState, random, searchSpace, problem);
    }
  }
}

public abstract record Evaluator<TGenotype, TSearchSpace, TExecutionState>
  : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new EvaluatorInstance(this, CreateInitialState());

  private sealed class EvaluatorInstance(Evaluator<TGenotype, TSearchSpace, TExecutionState> evaluator, TExecutionState executionState)
    : IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return evaluator.Evaluate(genotypes, executionState, random, searchSpace);
    }
  }
}

public abstract record Evaluator<TGenotype, TExecutionState>
  : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TExecutionState executionState,
    IRandomNumberGenerator random);

  public IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new EvaluatorInstance(this, CreateInitialState());

  private sealed class EvaluatorInstance(Evaluator<TGenotype, TExecutionState> evaluator, TExecutionState executionState)
    : IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return evaluator.Evaluate(genotypes, executionState, random);
    }
  }
}
