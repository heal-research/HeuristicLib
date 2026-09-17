using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every crossover to its observers and otherwise delegates to the wrapped crossover.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the crossover itself stays
/// agnostic so it can be used over any run for its candidate type. Observers written for a narrower search space
/// or problem than the run supplies are reported when the execution instance is created.
/// </remarks>
public sealed record ObservableCrossover<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingCrossover<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<ICrossoverObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableCrossover(ICrossover<TCandidate> childCrossover, params IReadOnlyList<ICrossoverObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childCrossover)
    {
        Observers = observers.ToValueArray();
    }

    protected override ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> childCrossover)
    {
        var observers = new ICrossoverObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not ICrossoverObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childCrossover, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, ICrossoverObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static ObservableCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> childCrossover, params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, observers);

    public static ObservableCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> childCrossover, Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));

    /// <summary>Observes offspring only.</summary>
    public static ObservableCrossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(ICrossover<TCandidate> childCrossover, Action<IReadOnlyList<TCandidate>> afterCross) =>
        new(childCrossover, new ActionCrossoverObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((offspring, _, _, _) => afterCross(offspring)));
}

public static class ObservableCrossoverExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(ICrossoverObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableCrossover<TCandidate, TSearchSpace, TProblem>(crossover, observer);
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableCrossover<TCandidate, TSearchSpace, TProblem>(crossover, observers);
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross) =>
            crossover.ObserveWith(new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));
    }

    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public ObservableCrossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCross) =>
            ObservableCrossover.Create(crossover, afterCross);
    }
}
