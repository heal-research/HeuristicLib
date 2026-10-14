using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Instrumentation;

public sealed class DurationMeasuringReplacerAdvice<TCandidate> : ReplacerAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringReplacerAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringReplacerAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IReplacer<TCandidate> replacer) =>
        childReplacer => new Execution<TRunSearchSpace, TRunProblem>(childReplacer, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem>(IReplacerExecution<TCandidate, TSearchSpace, TProblem> childReplacer, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingReplacerExecution<TCandidate, TSearchSpace, TProblem>(childReplacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
            IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
            ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public sealed record ReplacerDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<IReplacer<TCandidate>>
{
    public Advice<IReplacer<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringReplacerAdvice<TCandidate>(duration, timeProvider);
}

public static class ReplacerDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(IReplacer<TCandidate> replacer, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(replacer), new DurationMeasuringReplacerAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(IReplacer<TCandidate> replacer, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(replacer), new DurationMeasuringReplacerAdvice<TCandidate>(duration, timeProvider));
    }
}
