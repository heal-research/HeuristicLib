using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;

namespace RoleSlice;

public readonly struct SuccessfulCallHooks(CountAccumulator counter) : ICallHooks<Unit>, ISuccessHook
{
    public void OnSuccess(int candidateCount) => counter.IncrementBy(1);
    public Unit Enter() => default;
    public void Success(ref Unit token, int candidateCount) => counter.IncrementBy(1);
    public void Fault(ref Unit token, Exception exception) { }
    public void Finally(ref Unit token) { }
}
public readonly struct CandidateCountHooks(CountAccumulator counter) : ICallHooks<Unit>, ISuccessHook
{
    public void OnSuccess(int candidateCount) => counter.IncrementBy(candidateCount);
    public Unit Enter() => default;
    public void Success(ref Unit token, int candidateCount) => counter.IncrementBy(candidateCount);
    public void Fault(ref Unit token, Exception exception) { }
    public void Finally(ref Unit token) { }
}
public readonly struct DurationHooks(DurationAccumulator duration, TimeProvider time) : ICallHooks<long>
{
    public long Enter() => time.GetTimestamp();
    public void Success(ref long token, int candidateCount) { }
    public void Fault(ref long token, Exception exception) { }
    public void Finally(ref long token) => duration.AddDuration(time.GetElapsedTime(token));
}
public sealed class HookAdvice<TCandidate, TRole, THooks, TToken>(THooks hooks) : OperatorAdvice<TCandidate, TRole>
    where TRole : ISynchronousRole<TRole>
    where THooks : struct, ICallHooks<TToken>
{
    public override PreparedWrapper Prepare<TSearchSpace, TProblem>(IOperator<TCandidate, TRole> source, RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding) =>
        ((ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>)binding).Hooks<THooks, TToken>(hooks);
}
public sealed class SuccessAdvice<TCandidate, TRole, THooks>(THooks hook) : OperatorAdvice<TCandidate, TRole>
    where TRole : ISynchronousRole<TRole>
    where THooks : struct, ISuccessHook
{
    public override PreparedWrapper Prepare<TSearchSpace, TProblem>(IOperator<TCandidate, TRole> source, RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding) =>
        ((ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>)binding).Success(hook);
}
public sealed class PolicyAdvice<TCandidate, TRole>(SynchronousPolicy policy) : OperatorAdvice<TCandidate, TRole> where TRole : ISynchronousRole<TRole>
{
    public override PreparedWrapper Prepare<TSearchSpace, TProblem>(IOperator<TCandidate, TRole> source, RoleBinding<TCandidate, TRole, TSearchSpace, TProblem> binding) =>
        ((ISynchronousBinding<TCandidate, TRole, TSearchSpace, TProblem>)binding).PolicyWrapper(policy);
}
public sealed class CountingPolicy(CountAccumulator counter) : SynchronousPolicy
{
    public override CallResult<TResult> Execute<TExecution, TArguments, TResult>(ImmutableArray<TExecution> children, TArguments arguments, IRandomNumberGenerator random, Func<TExecution, TArguments, CallResult<TResult>> next)
    {
        var result = next(children[0], arguments);
        counter.IncrementBy(1);
        return result;
    }
}
public static class Inspection
{
    public static Aspect CountCalls<TCandidate, TRole>(this IOperator<TCandidate, TRole> source, CountAccumulator counter) where TRole : ISynchronousRole<TRole> =>
        Aspect.Create(Pointcut.Reference(source), new SuccessAdvice<TCandidate, TRole, SuccessfulCallHooks>(new(counter)));
    public static Aspect CountCalls<TCandidate, TRole>(this IOperator<TCandidate, TRole> source, out CountAccumulator counter) where TRole : ISynchronousRole<TRole>
    {
        counter = new();
        return source.CountCalls(counter);
    }
    public static Aspect CountCandidates<TCandidate, TRole>(this IOperator<TCandidate, TRole> source, CountAccumulator counter) where TRole : ISynchronousRole<TRole>, ICandidateCountingRole =>
        Aspect.Create(Pointcut.Reference(source), new SuccessAdvice<TCandidate, TRole, CandidateCountHooks>(new(counter)));
    public static Aspect MeasureDuration<TCandidate, TRole>(this IOperator<TCandidate, TRole> source, DurationAccumulator duration, TimeProvider time) where TRole : ISynchronousRole<TRole> =>
        Aspect.Create(Pointcut.Reference(source), new HookAdvice<TCandidate, TRole, DurationHooks, long>(new(duration, time)));
}
public sealed class CandidateClock<TCandidate, TRole> : Clock<int> where TRole : ISynchronousRole<TRole>, ICandidateCountingRole
{
    public CountAccumulator Counter { get; } = new();
    public CandidateClock(IOperator<TCandidate, TRole> source) => Advices = source.CountCandidates(Counter).Advices;
    protected override int ReadTime() => Counter.CurrentCount;
}

public sealed record EvaluationObservation<TCandidate, TSearchSpace, TProblem>(IOperator<TCandidate, EvaluationRole> Source, IReadOnlyList<TCandidate> Candidates,
    IReadOnlyList<ObjectiveVector> Objectives, TSearchSpace SearchSpace, TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
public sealed class EvaluationObservationAdvice<TCandidate, TSearchSpace, TProblem>(Action<EvaluationObservation<TCandidate, TSearchSpace, TProblem>> observe) : OperatorAdvice<TCandidate, EvaluationRole>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override PreparedWrapper Prepare<TRunSearchSpace, TRunProblem>(IOperator<TCandidate, EvaluationRole> source, RoleBinding<TCandidate, EvaluationRole, TRunSearchSpace, TRunProblem> binding)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
            throw new InvalidOperationException("Observation context mismatch.");
        return PreparedWrapper.From<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>>(inner => inner.With(new Execution<TRunSearchSpace, TRunProblem>(source, inner.Operation, observe)));
    }
    private sealed class Execution<TRunSearchSpace, TRunProblem>(IOperator<TCandidate, EvaluationRole> source, IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> inner, Action<EvaluationObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace space, TRunProblem problem)
        {
            var result = inner.Evaluate(candidates, random, space, problem);
            observe(new(source, candidates, result, (TSearchSpace)(object)space, (TProblem)(object)problem));
            return result;
        }
    }
}
public static class EvaluationObservations
{
    public static Aspect ObserveEvaluations<TCandidate, TSearchSpace, TProblem>(this IOperator<TCandidate, EvaluationRole> source, Action<EvaluationObservation<TCandidate, TSearchSpace, TProblem>> observe)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => Aspect.Create(Pointcut.Reference(source), new EvaluationObservationAdvice<TCandidate, TSearchSpace, TProblem>(observe));
    public static Aspect ObserveEvaluations<TCandidate>(this IOperator<TCandidate, EvaluationRole> source, Action<EvaluationObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
        source.ObserveEvaluations<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(observe);
}

public sealed record AlgorithmObservation<TCandidate, TState>(IOperator<TCandidate, AlgorithmRole<TState>> Source, TState State, TState? PreviousState,
    int Sequence, IProblem<TCandidate, ISearchSpace<TCandidate>> Problem) : Observation<IProblem<TCandidate, ISearchSpace<TCandidate>>>(Problem) where TState : class, ISearchState;
public sealed class AlgorithmObservationAdvice<TCandidate, TState>(Action<AlgorithmObservation<TCandidate, TState>> observe) : OperatorAdvice<TCandidate, AlgorithmRole<TState>> where TState : class, ISearchState
{
    private sealed class SequenceState { public int Current; }
    public override PreparedWrapper Prepare<TSearchSpace, TProblem>(IOperator<TCandidate, AlgorithmRole<TState>> source, RoleBinding<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem> binding)
    {
        var sequence = new SequenceState();
        return PreparedWrapper.From<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>>(inner => inner.With(new Execution<TSearchSpace, TProblem>(source, inner.Operation, sequence, observe)));
    }
    private sealed class Execution<TSearchSpace, TProblem>(IOperator<TCandidate, AlgorithmRole<TState>> source, IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState> inner, SequenceState sequence, Action<AlgorithmObservation<TCandidate, TState>> observe)
        : IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public async IAsyncEnumerable<TState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previous = initialState;
            await foreach (var state in inner.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new(source, state, previous, Interlocked.Increment(ref sequence.Current), problem));
                previous = state;
                yield return state;
            }
        }
    }
}
public sealed class AlgorithmDurationAdvice<TCandidate, TState>(DurationAccumulator duration, TimeProvider time) : OperatorAdvice<TCandidate, AlgorithmRole<TState>> where TState : class, ISearchState
{
    public override PreparedWrapper Prepare<TSearchSpace, TProblem>(IOperator<TCandidate, AlgorithmRole<TState>> source, RoleBinding<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem> binding) =>
        PreparedWrapper.From<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>>(inner => inner.With(new Execution<TSearchSpace, TProblem>(inner.Operation, duration, time)));
    private sealed class Execution<TSearchSpace, TProblem>(IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState> inner, DurationAccumulator duration, TimeProvider time) : IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public async IAsyncEnumerable<TState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await using var iterator = inner.RunStreamingAsync(problem, random, initialState, ct).GetAsyncEnumerator(ct);
            while (true)
            {
                bool moved;
                var start = time.GetTimestamp();
                try
                { moved = await iterator.MoveNextAsync(); }
                finally { duration.AddDuration(time.GetElapsedTime(start)); }
                if (!moved)
                    yield break;
                yield return iterator.Current;
            }
        }
    }
}
public static class AlgorithmInspection
{
    public static Aspect ObserveStates<TCandidate, TState>(this IOperator<TCandidate, AlgorithmRole<TState>> source, Action<AlgorithmObservation<TCandidate, TState>> observe) where TState : class, ISearchState =>
        Aspect.Create(Pointcut.Reference(source), new AlgorithmObservationAdvice<TCandidate, TState>(observe));
    public static Aspect MeasureIterations<TCandidate, TState>(this IOperator<TCandidate, AlgorithmRole<TState>> source, DurationAccumulator duration, TimeProvider time) where TState : class, ISearchState =>
        Aspect.Create(Pointcut.Reference(source), new AlgorithmDurationAdvice<TCandidate, TState>(duration, time));
    public static TraceAnalyzer<ObjectiveVector> TraceBestQuality<TCandidate>(this IOperator<TCandidate, AlgorithmRole<PopulationState<TCandidate>>> source, params IReadOnlyList<Clock> clocks) =>
        TraceAnalyzer<ObjectiveVector>.Create<IOperator<TCandidate, AlgorithmRole<PopulationState<TCandidate>>>, AlgorithmObservation<TCandidate, PopulationState<TCandidate>>>(
            [source], static (algorithm, record) => algorithm.ObserveStates(record),
            static observation => observation.State.Population.Select(candidate => candidate.ObjectiveVector).Min(observation.Problem.Objective.TotalOrderComparer)!, clocks);
}
