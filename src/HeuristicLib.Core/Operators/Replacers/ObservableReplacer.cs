using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public partial record ObservableReplacer<TG, TS, TP>
  : IReplacer<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public IReplacer<TG, TS, TP> Replacer { get; }

  [OrderedEquality] public ImmutableArray<IReplacerObserver<TG, TS, TP>> Observers { get; }

  public ObservableReplacer(IReplacer<TG, TS, TP> replacer, params ImmutableArray<IReplacerObserver<TG, TS, TP>> observers)
  {
    Replacer = replacer;
    Observers = observers;
  }

  public IReplacerInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var replacerInstance = instanceRegistry.Resolve(Replacer);
    var replacerObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new Instance(replacerInstance, replacerObserverInstances);
  }

  private sealed class Instance(IReplacerInstance<TG, TS, TP> replacerInstance, IReadOnlyList<IReplacerObserverInstance<TG, TS, TP>> observers)
    : IReplacerInstance<TG, TS, TP>
  {
    public IReadOnlyList<ISolution<TG>> Replace(IReadOnlyList<ISolution<TG>> previousPopulation, IReadOnlyList<ISolution<TG>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var result = replacerInstance.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterReplacement(result, previousPopulation, offspringPopulation, objective, searchSpace, problem);
      }

      return result;
    }
  }
}

public interface IReplacerObserver<in TG, in TS, in TP> : IExecutable<IReplacerObserverInstance<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>;

public interface IReplacerObserverInstance<in TG, in TS, in TP> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterReplacement(IReadOnlyList<ISolution<TG>> newPopulation, IReadOnlyList<ISolution<TG>> previousPopulation, IReadOnlyList<ISolution<TG>> offspringPopulation, Objective objective, TS searchSpace, TP problem);
}

public class ActionReplacerObserver<TG, TS, TP> : IReplacerObserver<TG, TS, TP>, IReplacerObserverInstance<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, TS, TP> afterReplacement;

  public ActionReplacerObserver(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, TS, TP> afterReplacement)
  {
    this.afterReplacement = afterReplacement;
  }

  public void AfterReplacement(IReadOnlyList<ISolution<TG>> newPopulation, IReadOnlyList<ISolution<TG>> previousPopulation, IReadOnlyList<ISolution<TG>> offspringPopulation, Objective objective, TS searchSpace, TP problem)
  {
    afterReplacement.Invoke(newPopulation, previousPopulation, offspringPopulation, searchSpace, problem);
  }

  public IReplacerObserverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
}

public static class ObservableReplacerExtensions
{
  extension<TG, TS, TP>(IReplacer<TG, TS, TP> replacer)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IReplacer<TG, TS, TP> ObserveWith(IReplacerObserver<TG, TS, TP> observer)
    {
      return new ObservableReplacer<TG, TS, TP>(replacer, observer);
    }

    public IReplacer<TG, TS, TP> ObserveWith(params ImmutableArray<IReplacerObserver<TG, TS, TP>> observers)
    {
      return new ObservableReplacer<TG, TS, TP>(replacer, observers);
    }

    public IReplacer<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, TS, TP> afterReplacement)
    {
      var observer = new ActionReplacerObserver<TG, TS, TP>(afterReplacement);
      return replacer.ObserveWith(observer);
    }

    public IReplacer<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>> afterReplacement)
    {
      var observer = new ActionReplacerObserver<TG, TS, TP>((newPopulation, _, _, _, _) => afterReplacement(newPopulation));
      return replacer.ObserveWith(observer);
    }

    public IReplacer<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return replacer.ObserveWith(_ => counter.IncrementBy(1));
    }

    public IReplacer<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return replacer.CountInvocations(counter);
    }
  }
}
