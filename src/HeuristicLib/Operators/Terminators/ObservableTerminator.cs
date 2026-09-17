using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every terminal state check to its observers and otherwise delegates to the wrapped terminator.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the terminator itself stays
/// agnostic so it can be used over any run for its candidate and search state.
/// </remarks>
public sealed record ObservableTerminator<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>
    : WrappingTerminator<TCandidate>
    where TObserverSearchState : class, ISearchState
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<ITerminatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>> Observers { get; init; }

    public ObservableTerminator(ITerminator<TCandidate> childTerminator, params IReadOnlyList<ITerminatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>> observers)
        : base(childTerminator)
    {
        Observers = observers.ToValueArray();
    }

    protected override ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator)
    {
        var observers = new ITerminatorObserver<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not ITerminatorObserver<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem, TObserverSearchState>(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TObserverSearchState> childTerminator, ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>[] observers)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(childTerminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TObserverSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TObserverSearchState state, TSearchSpace searchSpace, TProblem problem)
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
    public static ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState> Create<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(ITerminator<TCandidate> childTerminator, params IReadOnlyList<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>> observers)
        where TObserverSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, observers);

    public static ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState> Create<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(ITerminator<TCandidate> childTerminator, Action<bool, TObserverSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
        where TObserverSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(afterTerminalStateCheck));

    /// <summary>Observes the outcome only.</summary>
    public static ObservableTerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TObserverSearchState> Create<TCandidate, TObserverSearchState>(ITerminator<TCandidate> childTerminator, Action<bool> afterTerminalStateCheck)
        where TObserverSearchState : class, ISearchState =>
        new(childTerminator, new ActionTerminatorObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TObserverSearchState>((isTerminalState, _, _, _) => afterTerminalStateCheck(isTerminalState)));
}


public interface ITerminatorObserver<TCandidate, in TSearchSpace, in TProblem, in TObserverSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TObserverSearchState : class, ISearchState
{
    void AfterTerminalStateCheck(bool isTerminalState, TObserverSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(
    Action<bool, TObserverSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
    : ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TObserverSearchState : class, ISearchState
{
    public void AfterTerminalStateCheck(bool isTerminalState, TObserverSearchState state, TSearchSpace searchSpace, TProblem problem) =>
        afterTerminalStateCheck(isTerminalState, state, searchSpace, problem);
}

public static class ObservableTerminatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(ITerminator<TCandidate> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TObserverSearchState : class, ISearchState
    {
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState> ObserveWith(ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState> observer) =>
            new ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(terminator, observer);
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState> ObserveWith(params IReadOnlyList<ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>> observers) =>
            new ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(terminator, observers);
        public ObservableTerminator<TCandidate, TSearchSpace, TProblem, TObserverSearchState> ObserveWith(Action<bool, TObserverSearchState, TSearchSpace, TProblem> afterTerminalStateCheck) =>
            terminator.ObserveWith(new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(afterTerminalStateCheck));
    }

    /// <remarks>
    /// The observer reads only whether the state was terminal, so it is written at the widest state as well as the
    /// widest search space and problem. Observers are contravariant in all three, so this one serves any run — and
    /// every argument comes from the receiver, leaving nothing for a call site to name.
    /// </remarks>
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public ObservableTerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> ObserveWith(Action<bool> afterTerminalStateCheck) =>
            ObservableTerminator.Create<TCandidate, ISearchState>(terminator, afterTerminalStateCheck);
    }
}
