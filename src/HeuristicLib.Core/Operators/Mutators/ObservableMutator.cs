using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public class ObservableMutator<TG, TS, TP>
  : Mutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IMutator<TG, TS, TP> mutator;
  private readonly IReadOnlyList<IMutatorObserver<TG, TS, TP>> observers;
  
  public ObservableMutator(IMutator<TG, TS, TP> mutator, params IReadOnlyList<IMutatorObserver<TG, TS, TP>> observers)
  {
    this.mutator = mutator;
    this.observers = observers;
  }
  
  public override IMutatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var mutatorInstance = instanceRegistry.GetOrCreate(mutator);
    return new ObservableMutatorInstance(mutatorInstance, observers);
  }

  private sealed class ObservableMutatorInstance(IMutatorInstance<TG, TS, TP> mutatorInstance, IReadOnlyList<IMutatorObserver<TG, TS, TP>> observers) 
    : MutatorInstance<TG, TS, TP>
  {
    public override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var result = mutatorInstance.Mutate(parent, random, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterMutate(result, parent, random, searchSpace, problem);
      }
      
      return result;
    }
  }
}

public interface IMutatorObserver<in TG, in TS, in TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: probably remove the random for observation
  void AfterMutate(IReadOnlyList<TG> offspring, IReadOnlyCollection<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

// ToDo: rename to make it clear that this is not a base-class to be inherited from
public class MutatorObserver<TG, TS, TP> : IMutatorObserver<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, IReadOnlyCollection<TG>, IRandomNumberGenerator, TS, TP> afterMutate;
  
  public MutatorObserver(Action<IReadOnlyList<TG>, IReadOnlyCollection<TG>, IRandomNumberGenerator, TS, TP> afterMutate)
  {
    this.afterMutate = afterMutate;
  }
  
  public void AfterMutate(IReadOnlyList<TG> offspring, IReadOnlyCollection<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    afterMutate.Invoke(offspring, parent, random, searchSpace, problem);
  }
}

public static class ObservableMutatorExtensions
{
  extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IMutator<TG, TS, TP> ObserveWith(IMutatorObserver<TG, TS, TP> observer)
    {
      return new ObservableMutator<TG, TS, TP>(mutator, observer);
    }
    
    public IMutator<TG, TS, TP> ObserveWith(params IReadOnlyList<IMutatorObserver<TG, TS, TP>> observers)
    {
      return new ObservableMutator<TG, TS, TP>(mutator, observers);
    }
    
    public IMutator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyCollection<TG>, IRandomNumberGenerator, TS, TP> afterMutate)
    {
      var observer = new MutatorObserver<TG, TS, TP>(afterMutate);
      return mutator.ObserveWith(observer);
    }
    
    public IMutator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterMutate)
    {
      var observer = new MutatorObserver<TG, TS, TP>((offspring, _, _, _, _) => afterMutate(offspring));
      return mutator.ObserveWith(observer);
    }
    
    public IMutator<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return mutator.ObserveWith(offspring => counter.IncrementBy(offspring.Count));
    }
    
    public IMutator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return mutator.CountInvocations(counter);
    }
  }
}
