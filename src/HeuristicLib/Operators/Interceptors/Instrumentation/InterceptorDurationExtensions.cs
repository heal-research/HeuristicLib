using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class InterceptorDurationExtensions
{
    extension<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TR : class, ISearchState
    {
        public IInterceptor<TG, TS, TP, TR> MeasureInterceptorDuration(ObservationDuration duration)
            => interceptor.MeasureInterceptorDuration(duration, TimeProvider.System);

        public IInterceptor<TG, TS, TP, TR> MeasureInterceptorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringInterceptor<TG, TS, TP, TR>(interceptor, duration, timeProvider);

        public IInterceptor<TG, TS, TP, TR> MeasureInterceptorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return interceptor.MeasureInterceptorDuration(duration);
        }

        public IInterceptor<TG, TS, TP, TR> MeasureInterceptorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return interceptor.MeasureInterceptorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringInterceptor<TG, TS, TP, TR>
        : WrappingInterceptor<TG, TS, TP, TR>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TR : class, ISearchState
    {
        public DurationMeasuringInterceptor(
            IInterceptor<TG, TS, TP, TR> interceptor,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(interceptor)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override TR Transform(
            TR currentState,
            TR? previousState,
            InnerTransform innerTransform,
            TS searchSpace,
            TP problem)
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
