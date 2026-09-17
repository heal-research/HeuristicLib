using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every creation to its observers and otherwise delegates to the wrapped creator.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the creator itself stays
/// agnostic so it can be used over any run for its candidate type. Observers written for a narrower search space
/// or problem than the run supplies are reported when the execution instance is created.
/// </remarks>
public sealed record ObservableCreator<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingCreator<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<ICreatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableCreator(ICreator<TCandidate> childCreator, params IReadOnlyList<ICreatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childCreator)
    {
        Observers = observers.ToValueArray();
    }

    protected override ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childCreator)
    {
        var observers = new ICreatorObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not ICreatorObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childCreator, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator, ICreatorObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static ObservableCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> childCreator, params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, observers);

    public static ObservableCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> childCreator, Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));

    /// <summary>Observes candidates only.</summary>
    public static ObservableCreator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(ICreator<TCandidate> childCreator, Action<IReadOnlyList<TCandidate>> afterCreation) =>
        new(childCreator, new ActionCreatorObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((candidates, _, _, _) => afterCreation(candidates)));
}

public static class ObservableCreatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(ICreatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observer);
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observers);
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation) =>
            creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));
    }

    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public ObservableCreator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCreation) =>
            ObservableCreator.Create(creator, afterCreation);
    }
}
