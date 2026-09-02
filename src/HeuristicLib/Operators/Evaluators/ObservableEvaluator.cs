using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every evaluation to its observers and otherwise delegates to the wrapped evaluator.
/// </summary>
public sealed record ObservableEvaluator<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingEvaluator<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<IEvaluatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableEvaluator(IEvaluator<TCandidate> childEvaluator, params IReadOnlyList<IEvaluatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childEvaluator)
    {
        Observers = observers.ToValueArray();
    }

    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator)
    {
        var observers = new IEvaluatorObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not IEvaluatorObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childEvaluator, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterEvaluation(result, candidates, searchSpace, problem);
            }

            return result;
        }
    }
}

public static class ObservableEvaluator
{
    public static ObservableEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> childEvaluator, params IReadOnlyList<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, observers);

    public static ObservableEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> childEvaluator, Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterEvaluation)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));

    /// <summary>Observes results only, so the observer is written at the widest search space and problem.</summary>
    public static ObservableEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>> afterEvaluation) =>
        new(childEvaluator, new ActionEvaluatorObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((objectiveVectors, candidates, _, _) => afterEvaluation(objectiveVectors, candidates)));
}

public interface IEvaluatorObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterEvaluation(IReadOnlyList<ObjectiveVector> objectiveVectors, IReadOnlyList<TCandidate> candidates, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterEvaluation)
    : IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterEvaluation(IReadOnlyList<ObjectiveVector> objectiveVectors, IReadOnlyList<TCandidate> candidates, TSearchSpace searchSpace, TProblem problem) =>
        afterEvaluation(objectiveVectors, candidates, searchSpace, problem);
}

public static class ObservableEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observer);
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observers);
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterEvaluation) =>
            evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));
    }

    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public ObservableEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>> afterEvaluation) =>
            ObservableEvaluator.Create(evaluator, afterEvaluation);
    }
}
