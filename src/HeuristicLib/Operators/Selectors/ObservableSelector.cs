using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record ObservableSelector<TCandidate, TSearchSpace, TProblem>
    : WrappingSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; init; }

    public ObservableSelector(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, params IReadOnlyList<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(childSelector)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector) =>
        new Instance(childSelector, Observers);

    private sealed class Instance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, ValueArray<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
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
    public static ObservableSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, params IReadOnlyList<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, observers);

    public static ObservableSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));

    public static ObservableSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterSelection)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>((selected, _, _, _, _, _) => afterSelection(selected)));
}

public static class ObservableSelectorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(ISelectorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableSelector<TCandidate, TSearchSpace, TProblem>(selector, observer);
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableSelector<TCandidate, TSearchSpace, TProblem>(selector, observers);
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection) =>
            selector.ObserveWith(new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterSelection) =>
            selector.ObserveWith(new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>((selected, _, _, _, _, _) => afterSelection(selected)));
    }
}
