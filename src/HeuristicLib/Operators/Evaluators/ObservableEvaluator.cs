using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

[Equatable]
public partial record ObservableEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator => InnerEvaluator;

    [OrderedEquality]
    public ImmutableArray<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; }

    public ObservableEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, ImmutableArray<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(evaluator)
    {
        Observers = observers;
    }

    public ObservableEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, params IEnumerable<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
      : this(evaluator, [.. observers])
    {
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, Observers);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, ImmutableArray<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerEvaluator.Evaluate(candidates, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterEvaluation(candidates, result, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface IEvaluatorObserver<TCandidate, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterEvaluation(IReadOnlyList<TCandidate> candidates, IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, TSearchSpace, TProblem> afterEvaluation)
    : IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterEvaluation(IReadOnlyList<TCandidate> candidates, IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates, TSearchSpace searchSpace, TProblem problem) =>
        afterEvaluation(candidates, evaluatedCandidates, searchSpace, problem);
}

public static class ObservableEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observer);
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IEnumerable<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observers);
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, TSearchSpace, TProblem> afterEvaluation) =>
            evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));
        public ObservableEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterEvaluation) =>
            evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>((candidates, evaluatedCandidates, _, _) => afterEvaluation(candidates, evaluatedCandidates)));
    }
}
