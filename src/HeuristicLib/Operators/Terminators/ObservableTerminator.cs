using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; init; }

    public ObservableTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, params IReadOnlyList<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : base(terminator)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator) =>
        new Instance(childTerminator, Observers);

    private sealed class Instance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ValueArray<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildTerminator.IsTerminalState(state, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterTerminalStateCheck(result, state, searchSpace, problem);
            }
            return result;
        }
    }
}

public static class ObservableTerminator
{
    public static ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, params IReadOnlyList<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, observers);

    public static ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterTerminalStateCheck));

    public static ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, Action<bool> afterTerminalStateCheck)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((isTerminalState, _, _, _) => afterTerminalStateCheck(isTerminalState)));
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
