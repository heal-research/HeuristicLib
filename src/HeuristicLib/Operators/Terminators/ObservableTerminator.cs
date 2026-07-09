using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  : WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality] public ImmutableArray<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; }

    public ObservableTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, ImmutableArray<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
      : base(terminator)
    {
        Observers = observers;
    }

    public ObservableTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, params IEnumerable<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
      : this(terminator, [.. observers])
    {
    }

    protected override bool IsTerminalState(TSearchState searchState, InnerIsTerminalState innerIsTerminalState, TSearchSpace searchSpace, TProblem problem)
    {
        var result = innerIsTerminalState(searchState, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterTerminalStateCheck(result, searchState, searchSpace, problem);
        }
        return result;
    }
}


public interface ITerminatorObserver<in TCandidate, in TSearchSpace, in TProblem, in TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    void AfterTerminalStateCheck(bool isTerminalState, TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public static class ObservableTerminatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
      where TSearchState : class, ISearchState
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          => new ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, observer);
        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(params IEnumerable<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
          => new ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, observers);
        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
          => terminator.ObserveWith(new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterTerminalStateCheck));
        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<bool> afterTerminalStateCheck)
          => terminator.ObserveWith(new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((isTerminalState, _, _, _) => afterTerminalStateCheck(isTerminalState)));
    }
}

public sealed class ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck) : ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    public void AfterTerminalStateCheck(bool isTerminalState, TSearchState state, TSearchSpace searchSpace, TProblem problem) => afterTerminalStateCheck(isTerminalState, state, searchSpace, problem);
}
