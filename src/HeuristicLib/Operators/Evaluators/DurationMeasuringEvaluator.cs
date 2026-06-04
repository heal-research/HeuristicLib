using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record DurationMeasuringEvaluator<TG, TS, TP>
    : WrappingEvaluator<TG, TS, TP>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public DurationMeasuringEvaluator(
        IEvaluator<TG, TS, TP> evaluator,
        ObservationDuration duration,
        TimeProvider timeProvider)
        : base(evaluator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<TG> genotypes,
        InnerEvaluate innerEvaluate,
        IRandomNumberGenerator random,
        TS searchSpace,
        TP problem)
    {
        var startTimestamp = TimeProvider.GetTimestamp();
        try
        {
            return innerEvaluate(genotypes, random, searchSpace, problem);
        }
        finally
        {
            Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
        }
    }
}
