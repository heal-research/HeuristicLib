using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record ObservableEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; init; }

    public ObservableEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, params IReadOnlyList<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(childEvaluator)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator) =>
        new Instance(childEvaluator, Observers);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, ValueArray<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
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
    public static ObservableEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, params IReadOnlyList<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, observers);

    public static ObservableEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterEvaluation)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));

    public static ObservableEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>> afterEvaluation)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>((objectiveVectors, candidates, _, _) => afterEvaluation(objectiveVectors, candidates)));
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
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observer);
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observers);
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterEvaluation) =>
            evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>> afterEvaluation) =>
            evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>((objectiveVectors, candidates, _, _) => afterEvaluation(objectiveVectors, candidates)));
    }
}
