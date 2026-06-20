using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public static class TerminatorDurationExtensions
{
    extension<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TR : class, ISearchState
    {
        public ITerminator<TG, TS, TP, TR> MeasureTerminatorDuration(ObservationDuration duration)
            => terminator.MeasureTerminatorDuration(duration, TimeProvider.System);

        public ITerminator<TG, TS, TP, TR> MeasureTerminatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringTerminator<TG, TS, TP, TR>(terminator, duration, timeProvider);

        public ITerminator<TG, TS, TP, TR> MeasureTerminatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return terminator.MeasureTerminatorDuration(duration);
        }

        public ITerminator<TG, TS, TP, TR> MeasureTerminatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return terminator.MeasureTerminatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringTerminator<TG, TS, TP, TR>
        : WrappingTerminator<TG, TS, TP, TR>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TR : class, ISearchState
    {
        public DurationMeasuringTerminator(
            ITerminator<TG, TS, TP, TR> terminator,
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
            TR searchState,
            InnerIsTerminalState innerIsTerminalState,
            TS searchSpace,
            TP problem)
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
