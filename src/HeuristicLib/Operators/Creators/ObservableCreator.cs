using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record ObservableCreator<TCandidate, TSearchSpace, TProblem>
    : WrappingCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; init; }

    public ObservableCreator(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(childCreator)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator) =>
        new Instance(childCreator, Observers);

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator, ValueArray<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreator)
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildCreator.Create(count, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterCreation(result, count, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface ICreatorObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterCreation(IReadOnlyList<TCandidate> candidates, int count, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation)
    : ICreatorObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterCreation(IReadOnlyList<TCandidate> candidates, int count, TSearchSpace searchSpace, TProblem problem) =>
        afterCreation(candidates, count, searchSpace, problem);
}

public static class ObservableCreator
{
    public static ObservableCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, observers);

    public static ObservableCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));

    public static ObservableCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, Action<IReadOnlyList<TCandidate>> afterCreation)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>((candidates, _, _, _) => afterCreation(candidates)));
}

public static class ObservableCreatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(ICreatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observer);
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observers);
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation) =>
            creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCreation) =>
            creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>((candidates, _, _, _) => afterCreation(candidates)));
    }
}
