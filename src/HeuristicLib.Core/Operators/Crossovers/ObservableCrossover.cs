using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public class ObservableCrossover<TG, TS, TP>
  : Crossover<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly ICrossover<TG, TS, TP> crossover;
  private readonly IReadOnlyList<ICrossoverObserver<TG, TS, TP>> observers;
  
  public ObservableCrossover(ICrossover<TG, TS, TP> crossover, params IReadOnlyList<ICrossoverObserver<TG, TS, TP>> observers)
  {
    this.crossover = crossover;
    this.observers = observers;
  }
  
  public override ICrossoverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var crossoverInstance = instanceRegistry.GetOrCreate(crossover);
    return new ObservableCrossoverInstance(crossoverInstance, observers);
  }

  private sealed class ObservableCrossoverInstance(ICrossoverInstance<TG, TS, TP> crossoverInstance, IReadOnlyList<ICrossoverObserver<TG, TS, TP>> observers) 
    : CrossoverInstance<TG, TS, TP>
  {
    public override IReadOnlyList<TG> Cross(IReadOnlyList<IParents<TG>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var result = crossoverInstance.Cross(parents, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterCross(result, parents, random, searchSpace, problem);
      }
      
      return result;
    }
  }
}

public interface ICrossoverObserver<in TG, in TS, in TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: probably remove the random for observation
  void AfterCross(IReadOnlyList<TG> offspring, IReadOnlyCollection<IParents<TG>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

// ToDo: rename to make it clear that this is not a base-class to be inherited from
public class CrossoverObserver<TG, TS, TP> : ICrossoverObserver<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, IReadOnlyCollection<IParents<TG>>, IRandomNumberGenerator, TS, TP> afterCross;
  
  public CrossoverObserver(Action<IReadOnlyList<TG>, IReadOnlyCollection<IParents<TG>>, IRandomNumberGenerator, TS, TP> afterCross)
  {
    this.afterCross = afterCross;
  }
  
  public void AfterCross(IReadOnlyList<TG> offspring, IReadOnlyCollection<IParents<TG>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    afterCross.Invoke(offspring, parents, random, searchSpace, problem);
  }
}

public static class ObservableCrossoverExtensions
{
  extension<TG, TS, TP>(ICrossover<TG, TS, TP> crossover)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ICrossover<TG, TS, TP> ObserveWith(ICrossoverObserver<TG, TS, TP> observer)
    {
      return new ObservableCrossover<TG, TS, TP>(crossover, observer);
    }
    
    public ICrossover<TG, TS, TP> ObserveWith(params IReadOnlyList<ICrossoverObserver<TG, TS, TP>> observers)
    {
      return new ObservableCrossover<TG, TS, TP>(crossover, observers);
    }
    
    public ICrossover<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyCollection<IParents<TG>>, IRandomNumberGenerator, TS, TP> afterCross)
    {
      var observer = new CrossoverObserver<TG, TS, TP>(afterCross);
      return crossover.ObserveWith(observer);
    }
    
    public ICrossover<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterCross)
    {
      var observer = new CrossoverObserver<TG, TS, TP>((offspring, _, _, _, _) => afterCross(offspring));
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
