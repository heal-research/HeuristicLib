using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public class ObservableEvaluator<TG, TS, TP>
  : IEvaluator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IEvaluator<TG, TS, TP> evaluator;
  private readonly IReadOnlyList<IEvaluatorObserver<TG, TS, TP>> observers;

  public ObservableEvaluator(IEvaluator<TG, TS, TP> evaluator, params IReadOnlyList<IEvaluatorObserver<TG, TS, TP>> observers)
  {
    this.evaluator = evaluator;
    this.observers = observers;
  }

  public IEvaluatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrCreate(evaluator);
    return new ObservableEvaluatorInstance(evaluatorInstance, observers);
  }
  
  private sealed class ObservableEvaluatorInstance(IEvaluatorInstance<TG, TS, TP> evaluatorInstance, IReadOnlyList<IEvaluatorObserver<TG, TS, TP>> observers)
    : EvaluatorInstance<TG, TS, TP>
  {
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> genotypes, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var results = evaluatorInstance.Evaluate(genotypes, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterEvaluation(genotypes, results, random, searchSpace, problem);
      }
      
      return results;
    }
  }
}

public interface IEvaluatorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

public class FuncEvaluatorObserver<TG, TS, TP> : IEvaluatorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, IRandomNumberGenerator, TS, TP> afterEvaluation;

  public FuncEvaluatorObserver(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, IRandomNumberGenerator, TS, TP> afterEvaluation)
  {
    this.afterEvaluation = afterEvaluation;
  }

  public void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, IRandomNumberGenerator random, TS searchSpace, TP problem) =>
    afterEvaluation(genotypes, objectiveVectors, random, searchSpace, problem);
}

public static class ObservableEvaluatorExtensions
{
  extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IEvaluator<TG, TS, TP> ObserveWith(IEvaluatorObserver<TG, TS, TP> observer)
    {
      return new ObservableEvaluator<TG, TS, TP>(evaluator, observer);
    }

    public IEvaluator<TG, TS, TP> ObserveWith(params IReadOnlyList<IEvaluatorObserver<TG, TS, TP>> observers)
    {
      return new ObservableEvaluator<TG, TS, TP>(evaluator, observers);
    }
    
    public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, IRandomNumberGenerator, TS, TP> afterEvaluation)
    {
      var observer = new FuncEvaluatorObserver<TG, TS, TP>(afterEvaluation);
      return new ObservableEvaluator<TG, TS, TP>(evaluator, observer);
    }

    public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>> afterEvaluation)
    {
      var observer = new FuncEvaluatorObserver<TG, TS, TP>((genotypes, objectiveVectors, _, _, _) => afterEvaluation(genotypes, objectiveVectors));
      return new ObservableEvaluator<TG, TS, TP>(evaluator, observer);
    }

    public IEvaluator<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return evaluator.ObserveWith((solutions, objectives) => counter.IncrementBy(objectives.Count));
    }
    
    public IEvaluator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return evaluator.CountInvocations(counter);
    }
  }
}
