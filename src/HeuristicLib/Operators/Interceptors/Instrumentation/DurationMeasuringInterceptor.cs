using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record DurationMeasuringInterceptor<TCandidate>
    : WrappingInterceptor<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringInterceptor(IInterceptor<TCandidate> interceptor, ObservationDuration duration)
        : this(interceptor, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringInterceptor(IInterceptor<TCandidate> interceptor, ObservationDuration duration, TimeProvider timeProvider)
        : base(interceptor)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor) =>
        new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringInterceptor
{
    public static DurationMeasuringInterceptor<TCandidate> Create<TCandidate>(IInterceptor<TCandidate> childInterceptor, ObservationDuration duration) =>
        new(childInterceptor, duration);

    public static DurationMeasuringInterceptor<TCandidate> Create<TCandidate>(IInterceptor<TCandidate> childInterceptor, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childInterceptor, duration, timeProvider);
}

public static class InterceptorDurationExtensions
{
    extension<TCandidate>(IInterceptor<TCandidate> interceptor)
    {
        public DurationMeasuringInterceptor<TCandidate> MeasureInterceptorDuration(ObservationDuration duration) =>
            new(interceptor, duration);

        public DurationMeasuringInterceptor<TCandidate> MeasureInterceptorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(interceptor, duration, timeProvider);

        public DurationMeasuringInterceptor<TCandidate> MeasureInterceptorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(interceptor, duration);
        }

        public DurationMeasuringInterceptor<TCandidate> MeasureInterceptorDuration(
            out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(interceptor, duration, timeProvider);
        }
    }
}
