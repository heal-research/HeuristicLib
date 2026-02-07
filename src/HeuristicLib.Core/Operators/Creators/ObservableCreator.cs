using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public class ObservableCreator<TG, TS, TP>
  : Creator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly ICreator<TG, TS, TP> creator;
  private readonly IReadOnlyList<ICreatorObserver<TG, TS, TP>> observers;
  
  public ObservableCreator(ICreator<TG, TS, TP> creator, params IReadOnlyList<ICreatorObserver<TG, TS, TP>> observers)
  {
    this.creator = creator;
    this.observers = observers;
  }
  
  public override ICreatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var creatorInstance = instanceRegistry.GetOrCreate(creator);
    return new ObservableCreatorInstance(creatorInstance, observers);
  }

  private sealed class ObservableCreatorInstance(ICreatorInstance<TG, TS, TP> creatorInstance, IReadOnlyList<ICreatorObserver<TG, TS, TP>> observers) 
    : CreatorInstance<TG, TS, TP>
  {
    public override IReadOnlyList<TG> Create(int count, IRandomNumberGenerator random, TS searchSpace, TP problem)  {
      var result = creatorInstance.Create(count, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterCreation(result, count, random, searchSpace, problem);
      }
      
      return result;
    }
  }
}

public interface ICreatorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: probably remove the random for observation
  void AfterCreation(IReadOnlyList<TG> offspring, int count, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

// ToDo: rename to make it clear that this is not a base-class to be inherited from
public class CreatorObserver<TG, TS, TP> : ICreatorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, int, IRandomNumberGenerator, TS, TP> afterCreation;
  
  public CreatorObserver(Action<IReadOnlyList<TG>, int, IRandomNumberGenerator, TS, TP> afterCreation)
  {
    this.afterCreation = afterCreation;
  }
  
  public void AfterCreation(IReadOnlyList<TG> offspring, int count, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    afterCreation.Invoke(offspring, count, random, searchSpace, problem);
  }
}

public static class ObservableCreatorExtensions
{
  extension<TG, TS, TP>(ICreator<TG, TS, TP> creator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public ICreator<TG, TS, TP> ObserveWith(ICreatorObserver<TG, TS, TP> observer)
    {
      return new ObservableCreator<TG, TS, TP>(creator, observer);
    }
    
    public ICreator<TG, TS, TP> ObserveWith(params IReadOnlyList<ICreatorObserver<TG, TS, TP>> observers)
    {
      return new ObservableCreator<TG, TS, TP>(creator, observers);
    }
    
    public ICreator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, int, IRandomNumberGenerator, TS, TP> afterCreation)
    {
      var observer = new CreatorObserver<TG, TS, TP>(afterCreation);
      return creator.ObserveWith(observer);
    }
    
    public ICreator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterCreation)
    {
      var observer = new CreatorObserver<TG, TS, TP>((offspring, _, _, _, _) => afterCreation(offspring));
      return creator.ObserveWith(observer);
    }
    
    public ICreator<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return creator.ObserveWith(offspring => counter.IncrementBy(offspring.Count));
    }
    
    public ICreator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return creator.CountInvocations(counter);
    }
    
    // ToDo: think about how to implement this without knowing the start timing. Probably with a decorator on the CreatorInstance rather than a pure observer.
    public ICreator<TG, TS, TP> TimeInvocations(out InvocationTiming timing)
    {
      throw new NotImplementedException();
      var newTiming = new InvocationTiming();
      timing = newTiming;
      //return creator.ObserveWith(_ => ) 
      // // ToDo: think if we want the actual timer as dependency
      // var start = Stopwatch.GetTimestamp();
      // var result = @operator.Execute(input, context);
      // var duration = Stopwatch.GetElapsedTime(start);
      // timing.AddTime(duration);
      // return result;
    }
  }
}
