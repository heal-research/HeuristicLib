using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, ObservationDuration duration)
        : this(terminator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, ObservationDuration duration, TimeProvider timeProvider)
        : base(terminator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator) =>
        new Instance(childTerminator, Duration, TimeProvider);

    private sealed class Instance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
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
    public static DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationDuration duration)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, duration);

    public static DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childTerminator, duration, timeProvider);
}

public static class TerminatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(ObservationDuration duration) => new(terminator, duration);

        public DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(terminator, duration, timeProvider);

        public DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(terminator, duration);
        }

        public DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(terminator, duration, timeProvider);
        }
    }
}
