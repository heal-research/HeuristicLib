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
    public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator => InnerTerminator;

    [OrderedEquality] public ImmutableArray<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; }

    public ObservableTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, params IReadOnlyList<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : base(terminator)
    {
        Observers = observers.ToImmutableArray();
    }

    protected override WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator) =>
        new Instance(innerTerminator, Observers);

    private sealed class Instance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator, ImmutableArray<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(innerTerminator)
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerTerminator.IsTerminalState(state, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterTerminalStateCheck(result, state, searchSpace, problem);
            }
            return result;
        }
    }
}


public interface ITerminatorObserver<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    void AfterTerminalStateCheck(bool isTerminalState, TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(
    Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
    : ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void AfterTerminalStateCheck(bool isTerminalState, TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
        afterTerminalStateCheck(isTerminalState, state, searchSpace, problem);
}

public static class ObservableTerminatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer) =>
            new ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, observer);
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(params IReadOnlyList<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers) =>
            new ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, observers);
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck) =>
            terminator.ObserveWith(new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterTerminalStateCheck));
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<bool> afterTerminalStateCheck) =>
            terminator.ObserveWith(new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((isTerminalState, _, _, _) => afterTerminalStateCheck(isTerminalState)));
    }
}
