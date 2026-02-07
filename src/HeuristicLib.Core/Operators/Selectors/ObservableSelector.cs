using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class ObservableSelector<TG, TS, TP>
  : Selector<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly ISelector<TG, TS, TP> selector;
  private readonly IReadOnlyList<ISelectorObserver<TG, TS, TP>> observers;
  
  public ObservableSelector(ISelector<TG, TS, TP> selector, params IReadOnlyList<ISelectorObserver<TG, TS, TP>> observers)
  {
    this.selector = selector;
    this.observers = observers;
  }
  
  public override ISelectorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var selectorInstance = instanceRegistry.GetOrCreate(selector);
    return new ObservableSelectorInstance(selectorInstance, observers);
  }

  private sealed class ObservableSelectorInstance(ISelectorInstance<TG, TS, TP> selectorInstance, IReadOnlyList<ISelectorObserver<TG, TS, TP>> observers) 
    : SelectorInstance<TG, TS, TP>
  {
    public override IReadOnlyList<ISolution<TG>> Select(IReadOnlyList<ISolution<TG>> population, Objective objective, int count, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var result = selectorInstance.Select(population, objective, count, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterSelection(result, population, objective, count, searchSpace, problem);
      }
      
      return result;
    }
  }
}

public interface ISelectorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterSelection(IReadOnlyList<ISolution<TG>> selected, IReadOnlyList<ISolution<TG>> population, Objective objective, int count, TS searchSpace, TP problem);
}

// ToDo: rename to make it clear that this is not a base-class to be inherited from
public class SelectorObserver<TG, TS, TP> : ISelectorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection;
  
  public SelectorObserver(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
  {
    this.afterSelection = afterSelection;
  }

  public void AfterSelection(IReadOnlyList<ISolution<TG>> selected, IReadOnlyList<ISolution<TG>> population, Objective objective, int count,  TS searchSpace, TP problem) 
  {
    afterSelection.Invoke(selected, population, objective, count, searchSpace, problem);
  }
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
    
    public ISelector<TG, TS, TP> ObserveWith(params IReadOnlyList<ISelectorObserver<TG, TS, TP>> observers)
    {
      return new ObservableSelector<TG, TS, TP>(selector, observers);
    }
    
    public ISelector<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
    {
      var observer = new SelectorObserver<TG, TS, TP>(afterSelection);
      return selector.ObserveWith(observer);
    }
    
    public ISelector<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>> afterSelection)
    {
      var observer = new SelectorObserver<TG, TS, TP>((selected, _, _, _, _, _) => afterSelection(selected));
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
