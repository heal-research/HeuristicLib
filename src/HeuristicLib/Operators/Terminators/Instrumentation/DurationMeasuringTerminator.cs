using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record DurationMeasuringTerminator<TCandidate>
    : WrappingTerminator<TCandidate>
{
    public DurationAccumulator Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringTerminator(ITerminator<TCandidate> terminator, DurationAccumulator duration)
        : this(terminator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringTerminator(ITerminator<TCandidate> terminator, DurationAccumulator duration, TimeProvider timeProvider)
        : base(terminator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator) =>
        new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem, TSearchState>(ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingTerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildTerminator.IsTerminalState(state, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringTerminator
{
    public static DurationMeasuringTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, DurationAccumulator duration) =>
        new(childTerminator, duration);

    public static DurationMeasuringTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, DurationAccumulator duration, TimeProvider timeProvider) =>
        new(childTerminator, duration, timeProvider);
}

public static class TerminatorDurationExtensions
{
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public DurationMeasuringTerminator<TCandidate> MeasureDuration(DurationAccumulator duration) => new(terminator, duration);

        public DurationMeasuringTerminator<TCandidate> MeasureDuration(DurationAccumulator duration, TimeProvider timeProvider) =>
            new(terminator, duration, timeProvider);

        public DurationMeasuringTerminator<TCandidate> MeasureDuration(out DurationAccumulator duration)
        {
            duration = new DurationAccumulator();
            return new(terminator, duration);
        }

        public DurationMeasuringTerminator<TCandidate> MeasureDuration(out DurationAccumulator duration, TimeProvider timeProvider)
        {
            duration = new DurationAccumulator();
            return new(terminator, duration, timeProvider);
        }
    }
}
