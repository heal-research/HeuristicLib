using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every refinement to its observers and otherwise delegates to the wrapped refiner.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the refiner itself stays
/// agnostic so it can be used over any run for its candidate type.
/// </remarks>
public sealed record ObservableRefiner<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingRefiner<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<IRefinerObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableRefiner(IRefiner<TCandidate> childRefiner, params IReadOnlyList<IRefinerObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childRefiner)
    {
        Observers = observers.ToValueArray();
    }

    protected override IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> childRefiner)
    {
        var observers = new IRefinerObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not IRefinerObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childRefiner, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, IRefinerObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildRefiner.Refine(candidates, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterRefine(result, candidates, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface IRefinerObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterRefine(IReadOnlyList<TCandidate> refined, IReadOnlyList<TCandidate> candidates, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterRefine)
    : IRefinerObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterRefine(IReadOnlyList<TCandidate> refined, IReadOnlyList<TCandidate> candidates, TSearchSpace searchSpace, TProblem problem) =>
        afterRefine(refined, candidates, searchSpace, problem);
}

public static class ObservableRefiner
{
    public static ObservableRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> childRefiner, params IReadOnlyList<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, observers);

    public static ObservableRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        IRefiner<TCandidate> childRefiner,
        Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterRefine)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>(afterRefine));

    /// <summary>Observes refined candidates only.</summary>
    public static ObservableRefiner<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(
        IRefiner<TCandidate> childRefiner,
        Action<IReadOnlyList<TCandidate>> afterRefine) =>
        new(childRefiner, new ActionRefinerObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((refined, _, _, _) => afterRefine(refined)));
}

public static class ObservableRefinerExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(IRefinerObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableRefiner<TCandidate, TSearchSpace, TProblem>(refiner, observer);
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableRefiner<TCandidate, TSearchSpace, TProblem>(refiner, observers);
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterRefine) =>
            refiner.ObserveWith(new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>(afterRefine));
    }

    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public ObservableRefiner<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<TCandidate>> afterRefine) =>
            ObservableRefiner.Create(refiner, afterRefine);
    }
}
