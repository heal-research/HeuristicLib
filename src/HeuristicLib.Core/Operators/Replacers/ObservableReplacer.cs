using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public partial record ObservableReplacer<TG, TS, TP>
  : WrappingReplacer<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality]
  public ImmutableArray<IReplacerObserver<TG, TS, TP>> Observers { get; }

  public ObservableReplacer(IReplacer<TG, TS, TP> replacer, ImmutableArray<IReplacerObserver<TG, TS, TP>> observers)
    : base(replacer)
  {
    Observers = observers;
  }

  public ObservableReplacer(IReplacer<TG, TS, TP> replacer, params IEnumerable<IReplacerObserver<TG, TS, TP>> observers)
    : this(replacer, [.. observers])
  {
  }

  protected override IReadOnlyList<ISolution<TG>> Replace(IReadOnlyList<ISolution<TG>> previousPopulation, IReadOnlyList<ISolution<TG>> offspringPopulation, Objective objective, int count, InnerReplace innerReplace, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var result = innerReplace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
    foreach (var observer in Observers) {
      observer.AfterReplacement(result, previousPopulation, offspringPopulation, objective, searchSpace, problem);
    }
    return result;
  }
}

public interface IReplacerObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterReplacement(IReadOnlyList<ISolution<TG>> newPopulation, IReadOnlyList<ISolution<TG>> previousPopulation, IReadOnlyList<ISolution<TG>> offspringPopulation, Objective objective, TS searchSpace, TP problem);
}

public static class ObservableReplacerExtensions
{
  extension<TG, TS, TP>(IReplacer<TG, TS, TP> replacer)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IReplacer<TG, TS, TP> ObserveWith(IReplacerObserver<TG, TS, TP> observer)
      => new ObservableReplacer<TG, TS, TP>(replacer, observer);
    public IReplacer<TG, TS, TP> ObserveWith(params IEnumerable<IReplacerObserver<TG, TS, TP>> observers)
      => new ObservableReplacer<TG, TS, TP>(replacer, observers);
    public IReplacer<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, TS, TP> afterReplacement)
      => replacer.ObserveWith(new ActionReplacerObserver<TG, TS, TP>((newPopulation, previousPopulation, offspringPopulation, _, searchSpace, problem)
        => afterReplacement(newPopulation, previousPopulation, offspringPopulation, searchSpace, problem)));
    public IReplacer<TG, TS, TP> ObserveWith(Action<IReadOnlyList<ISolution<TG>>> afterReplacement)
      => replacer.ObserveWith(new ActionReplacerObserver<TG, TS, TP>((newPopulation, _, _, _, _, _) => afterReplacement(newPopulation)));
    public IReplacer<TG, TS, TP> CountInvocations(InvocationCounter counter)
      => replacer.ObserveWith(_ => counter.IncrementBy(1));
    public IReplacer<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return replacer.CountInvocations(counter);
    }
  }
}

public sealed class ActionReplacerObserver<TG, TS, TP>(Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, TS, TP> afterReplacement) : IReplacerObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public void AfterReplacement(IReadOnlyList<ISolution<TG>> newPopulation, IReadOnlyList<ISolution<TG>> previousPopulation, IReadOnlyList<ISolution<TG>> offspringPopulation, Objective objective, TS searchSpace, TP problem)
    => afterReplacement(newPopulation, previousPopulation, offspringPopulation, objective, searchSpace, problem);
}
