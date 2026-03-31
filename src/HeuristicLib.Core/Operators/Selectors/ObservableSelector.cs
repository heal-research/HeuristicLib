using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public partial record ObservableSelector<TG, TS, TP>
  : ISelector<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public ISelector<TG, TS, TP> Selector { get; }

  [OrderedEquality]
  public ImmutableArray<ISelectorObserver<TG, TS, TP>> Observers { get; }

  public ObservableSelector(ISelector<TG, TS, TP> selector, params ImmutableArray<ISelectorObserver<TG, TS, TP>> observers)
  {
    Selector = selector;
    Observers = observers;
  }

  public ISelectorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var selectorInstance = instanceRegistry.Resolve(Selector);
    var selectorObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new Instance(selectorInstance, selectorObserverInstances);
  }

  private sealed class Instance(ISelectorInstance<TG, TS, TP> selectorInstance, IReadOnlyList<ISelectorObserverInstance<TG, TS, TP>> observers)
    : ISelectorInstance<TG, TS, TP>
  {
    public IReadOnlyList<ISolution<TG>> Select(IReadOnlyList<ISolution<TG>> population, Objective objective, int count, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var result = selectorInstance.Select(population, objective, count, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterSelection(result, population, objective, count, searchSpace, problem);
      }

      return result;
    }
  }
}

public interface ISelectorObserver<in TG, in TS, in TP> : IExecutable<ISelectorObserverInstance<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>;

public interface ISelectorObserverInstance<in TG, in TS, in TP> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterSelection(IReadOnlyList<ISolution<TG>> selected, IReadOnlyList<ISolution<TG>> population, Objective objective, int count, TS searchSpace, TP problem);
}

public sealed class ActionSelectorObserver<TG, TS, TP> : ISelectorObserver<TG, TS, TP>, ISelectorObserverInstance<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection;

  public ActionSelectorObserver(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
  {
    this.afterSelection = afterSelection;
  }

  public void AfterSelection(IReadOnlyList<ISolution<TG>> selected, IReadOnlyList<ISolution<TG>> population, Objective objective, int count, TS searchSpace, TP problem)
  {
    afterSelection.Invoke(selected, population, objective, count, searchSpace, problem);
  }

  public ISelectorObserverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
}

public static class ObservableSelectorExtensions
{
  extension<TG, TS, TP>(ISelector<TG, TS, TP> selector)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ISelector<TG, TS, TP> ObserveWith(ISelectorObserver<TG, TS, TP> observer)
    {
      return new ObservableSelector<TG, TS, TP>(selector, observer);
    }

    public ISelector<TG, TS, TP> ObserveWith(params ImmutableArray<ISelectorObserver<TG, TS, TP>> observers)
    {
      return new ObservableSelector<TG, TS, TP>(selector, observers);
    }

    public ISelector<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
    {
      var observer = new ActionSelectorObserver<TG, TS, TP>(afterSelection);
      return selector.ObserveWith(observer);
    }

    public ISelector<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>> afterSelection)
    {
      var observer = new ActionSelectorObserver<TG, TS, TP>((selected, _, _, _, _, _) => afterSelection(selected));
      return selector.ObserveWith(observer);
    }

    public ISelector<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return selector.ObserveWith(_ => counter.IncrementBy(1));
    }

    public ISelector<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return selector.CountInvocations(counter);
    }
  }
}
