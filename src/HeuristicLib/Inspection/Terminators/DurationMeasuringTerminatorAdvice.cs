using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class DurationMeasuringTerminatorAdvice<TCandidate> : TerminatorAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringTerminatorAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringTerminatorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminator<TCandidate> terminator) =>
        childTerminator => new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, Duration, TimeProvider);

    private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingTerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
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

public sealed record TerminatorDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<ITerminator<TCandidate>>
{
    public Advice<ITerminator<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringTerminatorAdvice<TCandidate>(duration, timeProvider);
}

public static class TerminatorDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(ITerminator<TCandidate> terminator, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(terminator), new DurationMeasuringTerminatorAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(ITerminator<TCandidate> terminator, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(terminator), new DurationMeasuringTerminatorAdvice<TCandidate>(duration, timeProvider));
    }
}
