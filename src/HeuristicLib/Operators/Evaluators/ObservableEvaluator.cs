using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
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

    protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> genotypes, InnerEvaluate innerEvaluate, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
        var result = innerEvaluate(genotypes, random, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterEvaluation(genotypes, result, searchSpace, problem);
        }
        return result;
    }
}

public interface IEvaluatorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, TS searchSpace, TP problem);
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
        public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation)
          => evaluator.ObserveWith(new ActionEvaluatorObserver<TG, TS, TP>(afterEvaluation));
        public IEvaluator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>> afterEvaluation)
          => evaluator.ObserveWith(new ActionEvaluatorObserver<TG, TS, TP>((genotypes, objectiveVectors, _, _) => afterEvaluation(genotypes, objectiveVectors)));
        public IEvaluator<TG, TS, TP> CountEvaluatorCalls(ObservationCounter counter)
          => evaluator.ObserveWith((_, _) => counter.IncrementBy(1));
        public IEvaluator<TG, TS, TP> CountEvaluatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatorCalls(counter);
        }
        public IEvaluator<TG, TS, TP> CountEvaluatedGenotypes(ObservationCounter counter)
          => evaluator.ObserveWith((genotypes, _) => counter.IncrementBy(genotypes.Count));
        public IEvaluator<TG, TS, TP> CountEvaluatedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatedGenotypes(counter);
        }
        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(ObservationDuration duration)
          => evaluator.MeasureEvaluatorDuration(duration, TimeProvider.System);
        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(ObservationDuration duration, TimeProvider timeProvider)
          => new DurationMeasuringEvaluator<TG, TS, TP>(evaluator, duration, timeProvider);
        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return evaluator.MeasureEvaluatorDuration(duration);
        }
        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return evaluator.MeasureEvaluatorDuration(duration, timeProvider);
        }
    }
}

public sealed class ActionEvaluatorObserver<TG, TS, TP>(Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation) : IEvaluatorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    public void AfterEvaluation(IReadOnlyList<TG> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, TS searchSpace, TP problem) => afterEvaluation(genotypes, objectiveVectors, searchSpace, problem);
}
