using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record ObservableTerminator<TG, TR, TS, TP>
  : ITerminator<TG, TR, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public ITerminator<TG, TR, TS, TP> Terminator { get; }

  [OrderedEquality]
  public ImmutableArray<ITerminatorObserver<TG, TR, TS, TP>> Observers { get; }

  public ObservableTerminator(ITerminator<TG, TR, TS, TP> terminator, params ImmutableArray<ITerminatorObserver<TG, TR, TS, TP>> observers)
  {
    Terminator = terminator;
    Observers = observers;
  }

  public ITerminatorInstance<TG, TR, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var terminatorInstance = instanceRegistry.Resolve(Terminator);
    var terminatorObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new Instance(terminatorInstance, terminatorObserverInstances);
  }

  private sealed class Instance(ITerminatorInstance<TG, TR, TS, TP> terminatorInstance, IReadOnlyList<ITerminatorObserverInstance<TG, TS, TP, TR>> observers)
    : ITerminatorInstance<TG, TR, TS, TP>
  {
    public bool ShouldTerminate(TR state, TS searchSpace, TP problem)
    {
      var result = terminatorInstance.ShouldTerminate(state, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterTerminationCheck(result, state, searchSpace, problem);
      }

      return result;
    }
  }
}

public interface ITerminatorObserver<in TG, in TR, in TS, in TP> : IExecutable<ITerminatorObserverInstance<TG, TS, TP, TR>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState;

public interface ITerminatorObserverInstance<in TG, in TS, in TP, in TR> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  void AfterTerminationCheck(bool shouldTerminate, TR state, TS searchSpace, TP problem);
}

// Helper implementation for callback-based termination observers.
public class TerminatorObserver<TG, TR, TS, TP> : ITerminatorObserver<TG, TR, TS, TP>, ITerminatorObserverInstance<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly Action<bool, TR, TS, TP> afterTerminateCheck;

  public TerminatorObserver(Action<bool, TR, TS, TP> afterTerminateCheck)
  {
    this.afterTerminateCheck = afterTerminateCheck;
  }

  public void AfterTerminationCheck(bool shouldTerminate, TR state, TS searchSpace, TP problem)
  {
    afterTerminateCheck.Invoke(shouldTerminate, state, searchSpace, problem);
  }

  public ITerminatorObserverInstance<TG, TS, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
}

public static class ObservableTerminatorExtensions
{
  extension<TG, TR, TS, TP>(ITerminator<TG, TR, TS, TP> terminator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public ITerminator<TG, TR, TS, TP> ObserveWith(ITerminatorObserver<TG, TR, TS, TP> observer)
    {
      return new ObservableTerminator<TG, TR, TS, TP>(terminator, observer);
    }

    public ITerminator<TG, TR, TS, TP> ObserveWith(params ImmutableArray<ITerminatorObserver<TG, TR, TS, TP>> observers)
    {
      return new ObservableTerminator<TG, TR, TS, TP>(terminator, observers);
    }

    public ITerminator<TG, TR, TS, TP> ObserveWith(Action<bool, TR, TS, TP> afterTerminationCheck)
    {
      var observer = new TerminatorObserver<TG, TR, TS, TP>(afterTerminationCheck);
      return terminator.ObserveWith(observer);
    }

    public ITerminator<TG, TR, TS, TP> ObserveWith(Action<bool> afterTerminationCheck)
    {
      var observer = new TerminatorObserver<TG, TR, TS, TP>((shouldTerminate, _, _, _) => afterTerminationCheck(shouldTerminate));
      return terminator.ObserveWith(observer);
    }

    public ITerminator<TG, TR, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return terminator.ObserveWith(_ => counter.IncrementBy(1));
    }

    public ITerminator<TG, TR, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return terminator.CountInvocations(counter);
    }
  }
}
