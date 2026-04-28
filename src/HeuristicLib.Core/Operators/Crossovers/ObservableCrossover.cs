using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public partial record ObservableCrossover<TG, TS, TP>
  : WrappingCrossover<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality]
  public ImmutableArray<ICrossoverObserver<TG, TS, TP>> Observers { get; }

  public ObservableCrossover(ICrossover<TG, TS, TP> crossover, ImmutableArray<ICrossoverObserver<TG, TS, TP>> observers)
    : base(crossover)
  {
    Observers = observers;
  }

  public ObservableCrossover(ICrossover<TG, TS, TP> crossover, params IEnumerable<ICrossoverObserver<TG, TS, TP>> observers)
    : this(crossover, [.. observers])
  {
  }

  protected override IReadOnlyList<TG> Cross(IReadOnlyList<IParents<TG>> parents, InnerCross innerCross, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var result = innerCross(parents, random, searchSpace, problem);
    foreach (var observer in Observers) {
      observer.AfterCross(result, parents, searchSpace, problem);
    }
    return result;
  }
}

public interface ICrossoverObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterCross(IReadOnlyList<TG> offspring, IReadOnlyList<IParents<TG>> parents, TS searchSpace, TP problem);
}

public static class ObservableCrossoverExtensions
{
  extension<TG, TS, TP>(ICrossover<TG, TS, TP> crossover)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ICrossover<TG, TS, TP> ObserveWith(ICrossoverObserver<TG, TS, TP> observer)
      => new ObservableCrossover<TG, TS, TP>(crossover, observer);
    public ICrossover<TG, TS, TP> ObserveWith(params IEnumerable<ICrossoverObserver<TG, TS, TP>> observers)
      => new ObservableCrossover<TG, TS, TP>(crossover, observers);
    public ICrossover<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross)
      => crossover.ObserveWith(new ActionCrossoverObserver<TG, TS, TP>(afterCross));
    public ICrossover<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterCross)
      => crossover.ObserveWith(new ActionCrossoverObserver<TG, TS, TP>((offspring, _, _, _) => afterCross(offspring)));
    public ICrossover<TG, TS, TP> CountInvocations(InvocationCounter counter)
      => crossover.ObserveWith(_ => counter.IncrementBy(1));
    public ICrossover<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return crossover.CountInvocations(counter);
    }
  }
}

public sealed class ActionCrossoverObserver<TG, TS, TP>(Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross) : ICrossoverObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public void AfterCross(IReadOnlyList<TG> offspring, IReadOnlyList<IParents<TG>> parents, TS searchSpace, TP problem) => afterCross(offspring, parents, searchSpace, problem);
}
