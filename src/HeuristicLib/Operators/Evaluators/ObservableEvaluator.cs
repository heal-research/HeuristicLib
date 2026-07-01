using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

[Equatable]
public partial record ObservableEvaluator<TG, TS, TP>
  : WrappingEvaluator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    [OrderedEquality]
    public ImmutableArray<IEvaluatorObserver<TG, TS, TP>> Observers { get; }

    public ObservableEvaluator(IEvaluator<TG, TS, TP> evaluator, ImmutableArray<IEvaluatorObserver<TG, TS, TP>> observers)
      : base(evaluator)
    {
        Observers = observers;
    }

    public ObservableEvaluator(IEvaluator<TG, TS, TP> evaluator, params IEnumerable<IEvaluatorObserver<TG, TS, TP>> observers)
      : this(evaluator, [.. observers])
    {
    }

    protected override IReadOnlyList<Solution<TG>> Evaluate(IReadOnlyList<TG> genotypes, InnerEvaluate innerEvaluate, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
        var result = innerEvaluate(genotypes, random, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterEvaluation(genotypes, result, searchSpace, problem);
        }
        return result;
    }
}

public interface IEvaluatorObserver<TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<Solution<TG>> solutions, TS searchSpace, TP problem);
}

public static class ObservableEvaluatorExtensions
{
    extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
    {
        public IEvaluator<TG, TS, TP> ObserveWith(IEvaluatorObserver<TG, TS, TP> observer)
          => new ObservableEvaluator<TG, TS, TP>(evaluator, observer);
        public IEvaluator<TG, TS, TP> ObserveWith(params IEnumerable<IEvaluatorObserver<TG, TS, TP>> observers)
          => new ObservableEvaluator<TG, TS, TP>(evaluator, observers);
        public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<Solution<TG>>, TS, TP> afterEvaluation)
          => evaluator.ObserveWith(new ActionEvaluatorObserver<TG, TS, TP>(afterEvaluation));
        public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<Solution<TG>>> afterEvaluation)
          => evaluator.ObserveWith(new ActionEvaluatorObserver<TG, TS, TP>((genotypes, solutions, _, _) => afterEvaluation(genotypes, solutions)));
    }
}

public sealed class ActionEvaluatorObserver<TG, TS, TP>(Action<IReadOnlyList<TG>, IReadOnlyList<Solution<TG>>, TS, TP> afterEvaluation) : IEvaluatorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    public void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<Solution<TG>> solutions, TS searchSpace, TP problem) => afterEvaluation(genotypes, solutions, searchSpace, problem);
}
