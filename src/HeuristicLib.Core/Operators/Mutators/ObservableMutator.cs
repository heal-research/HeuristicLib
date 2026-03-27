using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record ObservableMutator<TG, TS, TP>
  : StatefulMutator<TG, TS, TP, ObservableMutator<TG, TS, TP>.State>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public IMutator<TG, TS, TP> Mutator { get; }

  [OrderedEquality] public ImmutableArray<IMutatorObserver<TG, TS, TP>> Observers { get; }

  public ObservableMutator(IMutator<TG, TS, TP> mutator, params ImmutableArray<IMutatorObserver<TG, TS, TP>> observers)
  {
    Mutator = mutator;
    Observers = observers;
  }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry)
  {
    var mutatorInstance = instanceRegistry.Resolve(Mutator);
    var mutatorObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new State(mutatorInstance, mutatorObserverInstances);
  }

  protected override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, State state, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var result = state.MutatorInstance.Mutate(parents, random, searchSpace, problem);

    foreach (var observerInstance in state.ObserverInstances) {
      observerInstance.AfterMutate(result, parents, searchSpace, problem);
    }

    return result;
  }
  
  public sealed class State(IMutatorInstance<TG, TS, TP> mutatorInstance, IReadOnlyList<IMutatorObserverInstance<TG, TS, TP>> observerInstances)
  {
    public IMutatorInstance<TG, TS, TP> MutatorInstance { get; } = mutatorInstance;
    public IReadOnlyList<IMutatorObserverInstance<TG, TS, TP>> ObserverInstances { get; } = observerInstances;
  }
}

public interface IMutatorObserver<in TG, in TS, in TP> : IExecutable<IMutatorObserverInstance<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>;

public interface IMutatorObserverInstance<in TG, in TS, in TP> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterMutate(IReadOnlyList<TG> offspring, IReadOnlyList<TG> parent, TS searchSpace, TP problem);
}

public sealed class ActionMutatorObserver<TG, TS, TP> : IMutatorObserver<TG, TS, TP>, IMutatorObserverInstance<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate;

  public ActionMutatorObserver(Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate)
  {
    this.afterMutate = afterMutate;
  }

  public void AfterMutate(IReadOnlyList<TG> offspring, IReadOnlyList<TG> parent, TS searchSpace, TP problem)
  {
    afterMutate.Invoke(offspring, parent, searchSpace, problem);
  }

  public IMutatorObserverInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
}

public static class ObservableMutatorExtensions
{
  extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IMutator<TG, TS, TP> ObserveWith(IMutatorObserver<TG, TS, TP> observer)
    {
      return new ObservableMutator<TG, TS, TP>(mutator, observer);
    }

    public IMutator<TG, TS, TP> ObserveWith(params ImmutableArray<IMutatorObserver<TG, TS, TP>> observers)
    {
      return new ObservableMutator<TG, TS, TP>(mutator, observers);
    }

    public IMutator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate)
    {
      var observer = new ActionMutatorObserver<TG, TS, TP>(afterMutate);
      return mutator.ObserveWith(observer);
    }

    public IMutator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterMutate)
    {
      var observer = new ActionMutatorObserver<TG, TS, TP>((offspring, _, _, _) => afterMutate(offspring));
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
