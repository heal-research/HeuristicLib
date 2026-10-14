using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Instrumentation;

public sealed class DurationMeasuringInterceptorAdvice<TCandidate> : InterceptorAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringInterceptorAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringInterceptorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutionFactory<IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptor<TCandidate> interceptor) =>
        childInterceptor => new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem, TSearchState>(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
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

public sealed record InterceptorDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<IInterceptor<TCandidate>>
{
    public Advice<IInterceptor<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringInterceptorAdvice<TCandidate>(duration, timeProvider);
}

public static class InterceptorDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(IInterceptor<TCandidate> interceptor, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(interceptor), new DurationMeasuringInterceptorAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(IInterceptor<TCandidate> interceptor, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(interceptor), new DurationMeasuringInterceptorAdvice<TCandidate>(duration, timeProvider));
    }
}
