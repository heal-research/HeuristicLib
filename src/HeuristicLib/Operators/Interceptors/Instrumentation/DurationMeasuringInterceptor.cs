using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

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

    protected override WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor) =>
        new Instance(childInterceptor, Duration, TimeProvider);

    private sealed class Instance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
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
    public static DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childInterceptor, duration);

    public static DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childInterceptor, duration, timeProvider);
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
            return new(interceptor, duration);
        }

        public DurationMeasuringInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> MeasureInterceptorDuration(
            out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(interceptor, duration, timeProvider);
        }
    }
}
