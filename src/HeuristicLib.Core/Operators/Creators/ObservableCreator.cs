using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

[Equatable]
public partial record ObservableCreator<TG, TS, TP>
  : WrappingCreator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality]
  public ImmutableArray<ICreatorObserver<TG, TS, TP>> Observers { get; }

  public ObservableCreator(ICreator<TG, TS, TP> creator, ImmutableArray<ICreatorObserver<TG, TS, TP>> observers)
    : base(creator)
  {
    Observers = observers;
  }

  public ObservableCreator(ICreator<TG, TS, TP> creator, params IEnumerable<ICreatorObserver<TG, TS, TP>> observers)
    : this(creator, [.. observers])
  {
  }

  protected override IReadOnlyList<TG> Create(int count, InnerCreate innerCreate, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var result = innerCreate(count, random, searchSpace, problem);
    foreach (var observer in Observers) {
      observer.AfterCreation(result, count, searchSpace, problem);
    }
    return result;
  }
}

public interface ICreatorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterCreation(IReadOnlyList<TG> offspring, int count, TS searchSpace, TP problem);
}

public sealed class ActionCreatorObserver<TG, TS, TP>(Action<IReadOnlyList<TG>, int, TS, TP> afterCreation) : ICreatorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public void AfterCreation(IReadOnlyList<TG> offspring, int count, TS searchSpace, TP problem) => afterCreation(offspring, count, searchSpace, problem);
}

public static class ObservableCreatorExtensions
{
  extension<TG, TS, TP>(ICreator<TG, TS, TP> creator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ICreator<TG, TS, TP> ObserveWith(ICreatorObserver<TG, TS, TP> observer)
      => new ObservableCreator<TG, TS, TP>(creator, observer);
    public ICreator<TG, TS, TP> ObserveWith(params IEnumerable<ICreatorObserver<TG, TS, TP>> observers)
      => new ObservableCreator<TG, TS, TP>(creator, observers);
    public ICreator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, int, TS, TP> afterCreation)
      => creator.ObserveWith(new ActionCreatorObserver<TG, TS, TP>(afterCreation));
    public ICreator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterCreation)
      => creator.ObserveWith(new ActionCreatorObserver<TG, TS, TP>((offspring, _, _, _) => afterCreation(offspring)));
    public ICreator<TG, TS, TP> CountInvocations(InvocationCounter counter)
      => creator.ObserveWith(_ => counter.IncrementBy(1));
    public ICreator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return creator.CountInvocations(counter);
    }
  }
}
