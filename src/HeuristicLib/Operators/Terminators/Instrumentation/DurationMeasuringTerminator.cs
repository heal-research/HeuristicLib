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
    public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator => InnerTerminator;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

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

    protected override WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(
        ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator) =>
        new Instance(innerTerminator, Duration, TimeProvider);

    private sealed class Instance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(innerTerminator)
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerTerminator.IsTerminalState(state, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
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
            return new DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, duration);
        }

        public DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, duration, timeProvider);
        }
    }
}
