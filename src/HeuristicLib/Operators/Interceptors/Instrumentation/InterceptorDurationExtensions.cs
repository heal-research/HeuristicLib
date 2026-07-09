using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class InterceptorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(ObservationDuration duration)
            => interceptor.MeasureInterceptorDuration(duration, TimeProvider.System);

        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, duration, timeProvider);

        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return interceptor.MeasureInterceptorDuration(duration);
        }

        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return interceptor.MeasureInterceptorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
        : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public DurationMeasuringInterceptor(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(interceptor)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override TSearchState Transform(
            TSearchState currentState,
            TSearchState? previousState,
            InnerTransform innerTransform,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerTransform(currentState, previousState, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
