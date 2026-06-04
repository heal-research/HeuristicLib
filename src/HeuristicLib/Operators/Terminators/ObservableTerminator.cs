using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record ObservableTerminator<TG, TS, TP, TR>
  : WrappingTerminator<TG, TS, TP, TR>
  where TR : class, ISearchState
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    [OrderedEquality] public ImmutableArray<ITerminatorObserver<TG, TS, TP, TR>> Observers { get; }

    public ObservableTerminator(ITerminator<TG, TS, TP, TR> terminator, ImmutableArray<ITerminatorObserver<TG, TS, TP, TR>> observers)
      : base(terminator)
    {
        Observers = observers;
    }

    public ObservableTerminator(ITerminator<TG, TS, TP, TR> terminator, params IEnumerable<ITerminatorObserver<TG, TS, TP, TR>> observers)
      : this(terminator, [.. observers])
    {
    }

    protected override bool IsTerminalState(TR searchState, InnerIsTerminalState innerIsTerminalState, TS searchSpace, TP problem)
    {
        var result = innerIsTerminalState(searchState, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterTerminalStateCheck(result, searchState, searchSpace, problem);
        }
        return result;
    }
}


public interface ITerminatorObserver<in TG, in TS, in TP, in TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
    void AfterTerminalStateCheck(bool isTerminalState, TR state, TS searchSpace, TP problem);
}

public static class ObservableTerminatorExtensions
{
    extension<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator)
      where TR : class, ISearchState
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
    {
        public ITerminator<TG, TS, TP, TR> ObserveWith(ITerminatorObserver<TG, TS, TP, TR> observer)
          => new ObservableTerminator<TG, TS, TP, TR>(terminator, observer);
        public ITerminator<TG, TS, TP, TR> ObserveWith(params IEnumerable<ITerminatorObserver<TG, TS, TP, TR>> observers)
          => new ObservableTerminator<TG, TS, TP, TR>(terminator, observers);
        public ITerminator<TG, TS, TP, TR> ObserveWith(Action<bool, TR, TS, TP> afterTerminalStateCheck)
          => terminator.ObserveWith(new ActionTerminatorObserver<TG, TS, TP, TR>(afterTerminalStateCheck));
        public ITerminator<TG, TS, TP, TR> ObserveWith(Action<bool> afterTerminalStateCheck)
          => terminator.ObserveWith(new ActionTerminatorObserver<TG, TS, TP, TR>((isTerminalState, _, _, _) => afterTerminalStateCheck(isTerminalState)));
        public ITerminator<TG, TS, TP, TR> CountTerminatorCalls(InvocationCounter counter)
          => terminator.ObserveWith(_ => counter.IncrementBy(1));
        public ITerminator<TG, TS, TP, TR> CountTerminatorCalls(out InvocationCounter counter)
        {
            counter = new InvocationCounter();
            return terminator.CountTerminatorCalls(counter);
        }
    }
}

public sealed class ActionTerminatorObserver<TG, TS, TP, TR>(Action<bool, TR, TS, TP> afterTerminalStateCheck) : ITerminatorObserver<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
    public void AfterTerminalStateCheck(bool isTerminalState, TR state, TS searchSpace, TP problem) => afterTerminalStateCheck(isTerminalState, state, searchSpace, problem);
}
