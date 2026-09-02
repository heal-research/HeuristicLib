using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every call to its observers and otherwise delegates to the wrapped selector.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the selector itself stays
/// agnostic so it can be used over any run for its candidate type. The two meet when the execution instance is
/// created: observers written for a wider search space or problem accept the run's, and a set written for a narrower
/// one is reported there rather than silently ignored.
/// </remarks>
public sealed record ObservableSelector<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingSelector<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<ISelectorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableSelector(ISelector<TCandidate> childSelector, params IReadOnlyList<ISelectorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childSelector)
    {
        Observers = observers.ToValueArray();
    }

    protected override ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> childSelector)
    {
        var observers = new ISelectorObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not ISelectorObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childSelector, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, ISelectorObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildSelector.Select(population, objective, count, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterSelection(result, population, objective, count, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface ISelectorObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterSelection(IReadOnlyList<EvaluatedCandidate<TCandidate>> selected, IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(
    Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection)
    : ISelectorObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterSelection(IReadOnlyList<EvaluatedCandidate<TCandidate>> selected, IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TSearchSpace searchSpace, TProblem problem) =>
        afterSelection(selected, population, objective, count, searchSpace, problem);
}

public static class ObservableSelector
{
    public static ObservableSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> childSelector, params IReadOnlyList<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, observers);

    public static ObservableSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> childSelector, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));

    /// <summary>Observes selected candidates only, so the observer is written at the widest search space and problem.</summary>
    public static ObservableSelector<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(ISelector<TCandidate> childSelector, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterSelection) =>
        new(childSelector, new ActionSelectorObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((selected, _, _, _, _, _) => afterSelection(selected)));
}

public static class ObservableSelectorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(ISelectorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableSelector<TCandidate, TSearchSpace, TProblem>(selector, observer);
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableSelector<TCandidate, TSearchSpace, TProblem>(selector, observers);
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection) =>
            selector.ObserveWith(new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));
    }

    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public ObservableSelector<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterSelection) =>
            ObservableSelector.Create(selector, afterSelection);
    }
}
