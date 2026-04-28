using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public partial record ObservableSelector<TG, TS, TP>
  : WrappingSelector<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality]
  public ImmutableArray<ISelectorObserver<TG, TS, TP>> Observers { get; }

  public ObservableSelector(ISelector<TG, TS, TP> selector, ImmutableArray<ISelectorObserver<TG, TS, TP>> observers)
    : base(selector)
  {
    Observers = observers;
  }

  public ObservableSelector(ISelector<TG, TS, TP> selector, params IEnumerable<ISelectorObserver<TG, TS, TP>> observers)
    : this(selector, [.. observers])
  {
  }


  protected override IReadOnlyList<ISolution<TG>> Select(IReadOnlyList<ISolution<TG>> population, Objective objective, int count, InnerSelect innerSelect, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var result = innerSelect(population, objective, count, random, searchSpace, problem);
    foreach (var observer in Observers) {
      observer.AfterSelection(result, population, objective, count, searchSpace, problem);
    }
    return result;
  }
}

public interface ISelectorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterSelection(IReadOnlyList<ISolution<TG>> selected, IReadOnlyList<ISolution<TG>> population, Objective objective, int count, TS searchSpace, TP problem);
}

public static class ObservableSelectorExtensions
{
  extension<TG, TS, TP>(ISelector<TG, TS, TP> selector)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ISelector<TG, TS, TP> ObserveWith(ISelectorObserver<TG, TS, TP> observer)
      => new ObservableSelector<TG, TS, TP>(selector, observer);
    public ISelector<TG, TS, TP> ObserveWith(params IEnumerable<ISelectorObserver<TG, TS, TP>> observers)
      => new ObservableSelector<TG, TS, TP>(selector, observers);
    public ISelector<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
      => selector.ObserveWith(new ActionSelectorObserver<TG, TS, TP>(afterSelection));
    public ISelector<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>> afterSelection)
      => selector.ObserveWith(new ActionSelectorObserver<TG, TS, TP>((selected, _, _, _, _, _) => afterSelection(selected)));
    public ISelector<TG, TS, TP> CountInvocations(InvocationCounter counter)
      => selector.ObserveWith(_ => counter.IncrementBy(1));
    public ISelector<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return selector.CountInvocations(counter);
    }
  }
}

public sealed class ActionSelectorObserver<TG, TS, TP>(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection) : ISelectorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public void AfterSelection(IReadOnlyList<ISolution<TG>> selected, IReadOnlyList<ISolution<TG>> population, Objective objective, int count, TS searchSpace, TP problem)
    => afterSelection(selected, population, objective, count, searchSpace, problem);
}
