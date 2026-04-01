using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record ObservableTerminator<TG, TS, TP, TR>
  : ITerminator<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public ITerminator<TG, TS, TP, TR> Terminator { get; }

  [OrderedEquality]
  public ImmutableArray<ITerminatorObserver<TG, TS, TP, TR>> Observers { get; }

  public ObservableTerminator(ITerminator<TG, TS, TP, TR> terminator, params ImmutableArray<ITerminatorObserver<TG, TS, TP, TR>> observers)
  {
    Terminator = terminator;
    Observers = observers;
  }

  public ITerminatorInstance<TG, TS, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var terminatorInstance = instanceRegistry.Resolve(Terminator);
    var terminatorObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new Instance(terminatorInstance, terminatorObserverInstances);
  }

  private sealed class Instance(ITerminatorInstance<TG, TS, TP, TR> terminatorInstance, IReadOnlyList<ITerminatorObserverInstance<TG, TS, TP, TR>> observers)
    : ITerminatorInstance<TG, TS, TP, TR>
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

public interface ITerminatorObserver<in TG, in TS, in TP, in TR> : IExecutable<ITerminatorObserverInstance<TG, TS, TP, TR>>
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
public class TerminatorObserver<TG, TS, TP, TR> : ITerminatorObserver<TG, TS, TP, TR>, ITerminatorObserverInstance<TG, TS, TP, TR>
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
  extension<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public ITerminator<TG, TS, TP, TR> ObserveWith(ITerminatorObserver<TG, TS, TP, TR> observer)
    {
      return new ObservableTerminator<TG, TS, TP, TR>(terminator, observer);
    }

    public ITerminator<TG, TS, TP, TR> ObserveWith(params ImmutableArray<ITerminatorObserver<TG, TS, TP, TR>> observers)
    {
      return new ObservableTerminator<TG, TS, TP, TR>(terminator, observers);
    }

    public ITerminator<TG, TS, TP, TR> ObserveWith(Action<bool, TR, TS, TP> afterTerminationCheck)
    {
      var observer = new TerminatorObserver<TG, TS, TP, TR>(afterTerminationCheck);
      return terminator.ObserveWith(observer);
    }

    public ITerminator<TG, TS, TP, TR> ObserveWith(Action<bool> afterTerminationCheck)
    {
      var observer = new TerminatorObserver<TG, TS, TP, TR>((shouldTerminate, _, _, _) => afterTerminationCheck(shouldTerminate));
      return terminator.ObserveWith(observer);
    }

    public ITerminator<TG, TS, TP, TR> CountInvocations(InvocationCounter counter)
    {
      return terminator.ObserveWith(_ => counter.IncrementBy(1));
    }

    public ITerminator<TG, TS, TP, TR> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return terminator.CountInvocations(counter);
    }
  }
}
