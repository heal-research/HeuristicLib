using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record ObservableCrossover<TCandidate, TSearchSpace, TProblem>
    : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; init; }

    public ObservableCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(childCrossover)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover) =>
        new Instance(childCrossover, Observers);

    private sealed class Instance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, ValueArray<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildCrossover.Cross(parents, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterCross(result, parents, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface ICrossoverObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterCross(IReadOnlyList<TCandidate> offspring, IReadOnlyList<Parents<TCandidate>> parents, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross)
    : ICrossoverObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterCross(IReadOnlyList<TCandidate> offspring, IReadOnlyList<Parents<TCandidate>> parents, TSearchSpace searchSpace, TProblem problem) =>
        afterCross(offspring, parents, searchSpace, problem);
}

public static class ObservableCrossover
{
    public static ObservableCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, observers);

    public static ObservableCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));

    public static ObservableCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, Action<IReadOnlyList<TCandidate>> afterCross)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterCross(offspring)));
}

public static class ObservableCrossoverExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(ICrossoverObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableCrossover<TCandidate, TSearchSpace, TProblem>(crossover, observer);
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableCrossover<TCandidate, TSearchSpace, TProblem>(crossover, observers);
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross) =>
            crossover.ObserveWith(new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCross) =>
            crossover.ObserveWith(new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterCross(offspring)));
    }
}
