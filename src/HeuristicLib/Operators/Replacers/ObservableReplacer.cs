using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every call to its observers and otherwise delegates to the wrapped replacer.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the replacer itself stays
/// agnostic so it can be used over any run for its candidate type. The two meet when the execution instance is
/// created: observers written for a wider search space or problem accept the run's, and a set written for a narrower
/// one is reported there rather than silently ignored.
/// </remarks>
public sealed record ObservableReplacer<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingReplacer<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<IReplacerObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableReplacer(IReplacer<TCandidate> childReplacer, params IReadOnlyList<IReplacerObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childReplacer)
    {
        Observers = observers.ToValueArray();
    }

    protected override IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> childReplacer)
    {
        var observers = new IReplacerObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not IReplacerObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childReplacer, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer, IReplacerObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterReplacement(result, previousPopulation, offspringPopulation, objective, searchSpace, problem);
            }

            return result;
        }
    }
}

public static class ObservableReplacer
{
    public static ObservableReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> childReplacer, params IReadOnlyList<IReplacerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childReplacer, observers);

    public static ObservableReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> childReplacer, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, TSearchSpace, TProblem> afterReplacement)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childReplacer, new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>(afterReplacement));

    /// <summary>Observes the resulting population only, so the observer is written at the widest search space and problem.</summary>
    public static ObservableReplacer<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(IReplacer<TCandidate> childReplacer, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterReplacement) =>
        new(childReplacer, new ActionReplacerObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((newPopulation, _, _, _, _, _) => afterReplacement(newPopulation)));
}

public interface IReplacerObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterReplacement(IReadOnlyList<EvaluatedCandidate<TCandidate>> newPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, TSearchSpace, TProblem> afterReplacement)
    : IReplacerObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterReplacement(IReadOnlyList<EvaluatedCandidate<TCandidate>> newPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, TSearchSpace searchSpace, TProblem problem) =>
        afterReplacement(newPopulation, previousPopulation, offspringPopulation, objective, searchSpace, problem);
}

public static class ObservableReplacerExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(IReplacerObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableReplacer<TCandidate, TSearchSpace, TProblem>(replacer, observer);
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IReplacerObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableReplacer<TCandidate, TSearchSpace, TProblem>(replacer, observers);
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, TSearchSpace, TProblem> afterReplacement) =>
            replacer.ObserveWith(new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>((newPopulation, previousPopulation, offspringPopulation, _, searchSpace, problem) => afterReplacement(newPopulation, previousPopulation, offspringPopulation, searchSpace, problem)));
    }

    extension<TCandidate>(IReplacer<TCandidate> replacer)
    {
        public ObservableReplacer<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterReplacement) =>
            ObservableReplacer.Create(replacer, afterReplacement);
    }
}
