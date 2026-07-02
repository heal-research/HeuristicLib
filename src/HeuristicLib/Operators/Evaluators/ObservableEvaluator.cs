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

    protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, InnerEvaluate innerEvaluate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var result = innerEvaluate(candidates, random, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterEvaluation(candidates, result, searchSpace, problem);
        }
        return result;
    }
}

public interface IEvaluatorObserver<in TCandidate, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterEvaluation(IReadOnlyList<TCandidate> candidates, IReadOnlyList<ObjectiveVector> objectiveVectors, TSearchSpace searchSpace, TProblem problem);
}

public static class ObservableEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          => new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observer);
        public IEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IEnumerable<IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
          => new ObservableEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, observers);
        public IEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<ObjectiveVector>, TSearchSpace, TProblem> afterEvaluation)
          => evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));
        public IEvaluator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<ObjectiveVector>> afterEvaluation)
          => evaluator.ObserveWith(new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>((candidates, objectiveVectors, _, _) => afterEvaluation(candidates, objectiveVectors)));
    }
}

public sealed class ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<ObjectiveVector>, TSearchSpace, TProblem> afterEvaluation) : IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterEvaluation(IReadOnlyList<TCandidate> candidates, IReadOnlyList<ObjectiveVector> objectiveVectors, TSearchSpace searchSpace, TProblem problem) => afterEvaluation(candidates, objectiveVectors, searchSpace, problem);
}
