using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record ObservableRefiner<TCandidate, TSearchSpace, TProblem>
    : WrappingRefiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; init; }

    public ObservableRefiner(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, params IReadOnlyList<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(childRefiner)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner) =>
        new Instance(childRefiner, Observers);

    private sealed class Instance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, ValueArray<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
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
    public static ObservableRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, params IReadOnlyList<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, observers);

    public static ObservableRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner,
        Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterRefine)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>(afterRefine));

    public static ObservableRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner,
        Action<IReadOnlyList<TCandidate>> afterRefine)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>((refined, _, _, _) => afterRefine(refined)));
}

public static class ObservableRefinerExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(IRefinerObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableRefiner<TCandidate, TSearchSpace, TProblem>(refiner, observer);
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IRefinerObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableRefiner<TCandidate, TSearchSpace, TProblem>(refiner, observers);
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterRefine) =>
            refiner.ObserveWith(new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>(afterRefine));
        public ObservableRefiner<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterRefine) =>
            refiner.ObserveWith(new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>((refined, _, _, _) => afterRefine(refined)));
    }
}
