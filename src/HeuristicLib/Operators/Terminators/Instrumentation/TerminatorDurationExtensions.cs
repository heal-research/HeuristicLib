using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public static class TerminatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(ObservationDuration duration)
            => terminator.MeasureTerminatorDuration(duration, TimeProvider.System);

        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, duration, timeProvider);

        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return terminator.MeasureTerminatorDuration(duration);
        }

        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureTerminatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return terminator.MeasureTerminatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
        : WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public DurationMeasuringTerminator(
            ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(terminator)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override bool IsTerminalState(
            TSearchState searchState,
            InnerIsTerminalState innerIsTerminalState,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerIsTerminalState(searchState, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
