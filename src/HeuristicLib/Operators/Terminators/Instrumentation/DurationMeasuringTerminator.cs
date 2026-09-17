using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record DurationMeasuringTerminator<TCandidate>
    : WrappingTerminator<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringTerminator(ITerminator<TCandidate> terminator, ObservationDuration duration)
        : this(terminator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringTerminator(ITerminator<TCandidate> terminator, ObservationDuration duration, TimeProvider timeProvider)
        : base(terminator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator) =>
        new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
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
    public static DurationMeasuringTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, ObservationDuration duration) =>
        new(childTerminator, duration);

    public static DurationMeasuringTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childTerminator, duration, timeProvider);
}

public static class TerminatorDurationExtensions
{
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public DurationMeasuringTerminator<TCandidate> MeasureDuration(ObservationDuration duration) => new(terminator, duration);

        public DurationMeasuringTerminator<TCandidate> MeasureDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(terminator, duration, timeProvider);

        public DurationMeasuringTerminator<TCandidate> MeasureDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(terminator, duration);
        }

        public DurationMeasuringTerminator<TCandidate> MeasureDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(terminator, duration, timeProvider);
        }
    }
}
