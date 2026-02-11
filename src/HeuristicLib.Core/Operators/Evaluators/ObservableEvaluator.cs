using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

[Equatable]
public partial record ObservableEvaluator<TG, TS, TP>
  : IEvaluator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public IEvaluator<TG, TS, TP> Evaluator { get; }

  [OrderedEquality] public ImmutableArray<IEvaluatorObserver<TG, TS, TP>> Observers { get; }

  public ObservableEvaluator(IEvaluator<TG, TS, TP> evaluator, params ImmutableArray<IEvaluatorObserver<TG, TS, TP>> observers)
  {
    Evaluator = evaluator;
    Observers = observers;
  }

  public IEvaluatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.Resolve(Evaluator);
    var evaluatorObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new ObservableEvaluatorInstance(evaluatorInstance, evaluatorObserverInstances);
  }

  private sealed class ObservableEvaluatorInstance(IEvaluatorInstance<TG, TS, TP> evaluatorInstance, IReadOnlyList<IEvaluatorObserverInstance<TG, TS, TP>> observers)
    : EvaluatorInstance<TG, TS, TP>
  {
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> genotypes, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var results = evaluatorInstance.Evaluate(genotypes, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterEvaluation(genotypes, results, searchSpace, problem);
      }

      return results;
    }
  }
}

public interface IEvaluatorObserver<in TG, in TS, in TP> : IExecutable<IEvaluatorObserverInstance<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>;

public interface IEvaluatorObserverInstance<in TG, in TS, in TP> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, TS searchSpace, TP problem);
}

public class ActionEvaluatorObserver<TG, TS, TP> : IEvaluatorObserver<TG, TS, TP>, IEvaluatorObserverInstance<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation;

  public ActionEvaluatorObserver(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation)
  {
    this.afterEvaluation = afterEvaluation;
  }

  public void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, TS searchSpace, TP problem) =>
    afterEvaluation(genotypes, objectiveVectors, searchSpace, problem);

  public IEvaluatorObserverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
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

    public IEvaluator<TG, TS, TP> ObserveWith(params ImmutableArray<IEvaluatorObserver<TG, TS, TP>> observers)
    {
      return new ObservableEvaluator<TG, TS, TP>(evaluator, observers);
    }

    public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation)
    {
      var observer = new ActionEvaluatorObserver<TG, TS, TP>(afterEvaluation);
      return new ObservableEvaluator<TG, TS, TP>(evaluator, observer);
    }

    public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>> afterEvaluation)
    {
      var observer = new ActionEvaluatorObserver<TG, TS, TP>((genotypes, objectiveVectors, _, _) => afterEvaluation(genotypes, objectiveVectors));
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
