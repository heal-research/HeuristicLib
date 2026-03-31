using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public partial record ObservableCrossover<TG, TS, TP>
  : ICrossover<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public ICrossover<TG, TS, TP> Crossover { get; }

  [OrderedEquality] public ImmutableArray<ICrossoverObserver<TG, TS, TP>> Observers { get; }

  public ObservableCrossover(ICrossover<TG, TS, TP> crossover, params ImmutableArray<ICrossoverObserver<TG, TS, TP>> observers)
  {
    Crossover = crossover;
    Observers = observers;
  }

  public ICrossoverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var crossoverInstance = instanceRegistry.Resolve(Crossover);
    var observerInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new Instance(crossoverInstance, observerInstances);
  }

  private sealed class Instance(ICrossoverInstance<TG, TS, TP> crossoverInstance, IReadOnlyList<ICrossoverObserverInstance<TG, TS, TP>> observers)
    : ICrossoverInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Cross(IReadOnlyList<IParents<TG>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var result = crossoverInstance.Cross(parents, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterCross(result, parents, searchSpace, problem);
      }

      return result;
    }
  }
}

public interface ICrossoverObserver<in TG, in TS, in TP>
  : IExecutable<ICrossoverObserverInstance<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>;

public interface ICrossoverObserverInstance<in TG, in TS, in TP> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterCross(IReadOnlyList<TG> offspring, IReadOnlyList<IParents<TG>> parents, TS searchSpace, TP problem);
}

public sealed class ActionCrossoverObserver<TG, TS, TP> : ICrossoverObserver<TG, TS, TP>, ICrossoverObserverInstance<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross;

  public ActionCrossoverObserver(Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross)
    => this.afterCross = afterCross;

  public void AfterCross(IReadOnlyList<TG> offspring, IReadOnlyList<IParents<TG>> parents, TS searchSpace, TP problem)
    => afterCross.Invoke(offspring, parents, searchSpace, problem);

  public ICrossoverObserverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => this;
}

public static class ObservableCrossoverExtensions
{
  extension<TG, TS, TP>(ICrossover<TG, TS, TP> crossover)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ICrossover<TG, TS, TP> ObserveWith(ICrossoverObserver<TG, TS, TP> observer)
    {
      return new ObservableCrossover<TG, TS, TP>(crossover, observer);
    }

    public ICrossover<TG, TS, TP> ObserveWith(params ImmutableArray<ICrossoverObserver<TG, TS, TP>> observers)
    {
      return new ObservableCrossover<TG, TS, TP>(crossover, observers);
    }

    public ICrossover<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross)
    {
      var observer = new ActionCrossoverObserver<TG, TS, TP>(afterCross);
      return crossover.ObserveWith(observer);
    }

    public ICrossover<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterCross)
    {
      var observer = new ActionCrossoverObserver<TG, TS, TP>((offspring, _, _, _) => afterCross(offspring));
      return crossover.ObserveWith(observer);
    }

    public ICrossover<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return crossover.ObserveWith(offspring => counter.IncrementBy(offspring.Count));
    }

    public ICrossover<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return crossover.CountInvocations(counter);
    }
  }
}
