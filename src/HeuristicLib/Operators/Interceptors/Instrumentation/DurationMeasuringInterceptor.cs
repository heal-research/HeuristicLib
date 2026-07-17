using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor => InnerInterceptor;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, ObservationDuration duration)
        : this(interceptor, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, ObservationDuration duration, TimeProvider timeProvider)
        : base(interceptor)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor) =>
        new Instance(innerInterceptor, Duration, TimeProvider);

    private sealed class Instance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(innerInterceptor)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerInterceptor.Transform(currentState, previousState, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class InterceptorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(ObservationDuration duration) =>
            new(interceptor, duration);

        public DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(interceptor, duration, timeProvider);

        public DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, duration);
        }

        public DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(
            out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, duration, timeProvider);
        }
    }
}
