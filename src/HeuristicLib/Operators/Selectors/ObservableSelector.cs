using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public partial record ObservableSelector<TCandidate, TSearchSpace, TProblem>
  : WrappingSelector<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector => InnerSelector;

    [OrderedEquality]
    public ImmutableArray<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; }

    public ObservableSelector(ISelector<TCandidate, TSearchSpace, TProblem> selector, ImmutableArray<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
      : base(selector)
    {
        Observers = observers;
    }

    public ObservableSelector(ISelector<TCandidate, TSearchSpace, TProblem> selector, params IEnumerable<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
      : this(selector, [.. observers])
    {
    }


    protected override WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> innerSelector) =>
        new Instance(innerSelector, Observers);

    private sealed class Instance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> innerSelector, ImmutableArray<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(innerSelector)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerSelector.Select(population, objective, count, random, searchSpace, problem);
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

public static class ObservableSelectorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(ISelectorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableSelector<TCandidate, TSearchSpace, TProblem>(selector, observer);
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(params IEnumerable<ISelectorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableSelector<TCandidate, TSearchSpace, TProblem>(selector, observers);
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection) =>
            selector.ObserveWith(new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));
        public ObservableSelector<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterSelection) =>
            selector.ObserveWith(new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>((selected, _, _, _, _, _) => afterSelection(selected)));
    }
}
