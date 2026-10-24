using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures.Authoring;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class OperatorLifecycleTests
{
    private static void Check(bool condition, string description) => Assert.True(condition, description);

    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));
    private static readonly RealVector[] Input = [new(2.0)];

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception failure)
        {
            return failure;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFailure(Exception failure) => throw failure;

    private static ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Typed(ResolutionScope scope) =>
        scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

    private static void InvokeOperation(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> operation) =>
        operation.Mutate(Input, RandomNumberGenerator.Create(4), Problem.SearchSpace, Problem);

    [Fact]
    public void FailureMatrix()
    {
        foreach (var childKind in new[] { "success", "failure", "cancellation" })
        {
            var probe = new Probe
            {
                ChildFailure = childKind switch
                {
                    "failure" => new InvalidOperationException("child failure"),
                    "cancellation" => new OperationCanceledException("child cancellation"),
                    _ => null
                }
            };
            var source = new ProbeMutation(probe);
            var operation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new LifecycleAdvice(probe)))).Resolve(source);
            var failure = Capture(() => InvokeOperation(operation));
            Check(ReferenceEquals(failure, probe.ChildFailure), "a child failure propagates unchanged and success returns normally");
            Check(probe.ChildFailure?.StackTrace?.Contains(nameof(ThrowFailure), StringComparison.Ordinal) != false,
                "the original throw-site stack survives the wrapper");
            Check(probe.Events.SequenceEqual(new[] { "enter:1", "child", probe.ChildFailure is null ? "success:1" : "fault:1", "finally:1" }),
                "the wrapper observes success or failure and cleans up exactly once");
            Check(probe.ChildCalls == 1 && probe.SuccessCalls == (probe.ChildFailure is null ? 1 : 0), "no child work is repeated");
        }

        var entryFailure = new InvalidOperationException("enter failure");
        var entry = new Probe { EntryFailure = entryFailure };
        var entrySource = new ProbeMutation(entry);
        var entryOperation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(entrySource), new LifecycleAdvice(entry)))).Resolve(entrySource);
        Check(ReferenceEquals(Capture(() => InvokeOperation(entryOperation)), entryFailure) && entry.Events.SequenceEqual(new[] { "enter:1" }) && entry.ChildCalls == 0,
            "a failure before Proceed invokes neither the child nor cleanup");

        var nestedFailure = new AggregateException(new InvalidOperationException("inner a"), new InvalidOperationException("inner b"));
        var nested = new Probe { ChildFailure = nestedFailure };
        var nestedSource = new ProbeMutation(nested);
        var nestedOperation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(nestedSource), new LifecycleAdvice(nested)))).Resolve(nestedSource);
        Check(ReferenceEquals(Capture(() => InvokeOperation(nestedOperation)), nestedFailure), "an aggregate child failure is neither wrapped nor flattened");
    }

    [Fact]
    public void NestedInvocations()
    {
        var probe = new Probe();
        var source = new ProbeMutation(probe);
        var operation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new LifecycleAdvice(probe)))).Resolve(source);
        var entered = false;
        probe.ChildAction = () =>
        {
            if (entered)
                return;
            entered = true;
            InvokeOperation(operation);
        };
        InvokeOperation(operation);
        Check(probe.Events.SequenceEqual(new[] { "enter:1", "child", "enter:2", "child", "success:2", "finally:2", "success:1", "finally:1" }),
            "nested calls keep their own invocation data");
    }

    [Fact]
    public void OperationsAndViews()
    {
        var probe = new Probe();
        var advice = new LifecycleAdvice(probe);
        var source = new SharedOperations(new ProbeLog());
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), advice));
        var mutation = Typed(scope).Resolve((IOperator<Mutation, RealVector>)source, out ILiveValue? control);
        control!.Value = 14;
        Check(mutation.Mutate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)[0][0] == 14,
            "invocation wrappers preserve the live state used by the callable operation");
        _ = Typed(scope).Resolve((IOperator<Evaluation, RealVector>)source).Evaluate([Input[0], Input[0]],
            RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);
        Check(Typed(scope).Resolve((IOperator<Checksum, RealVector>)source).Read(RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem) == 14, "an external scalar adapter uses the same wrapper");
        var broad = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var narrow = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        _ = broad.Resolve((IOperator<Termination, RealVector>)source).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
        _ = narrow.Resolve(((IOperator<Termination, RealVector>)source).Named("term")).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
        Check(advice.Wraps == 5 && source.Log.Preparations == 1, "one advice wraps every operation and state view of one prepared owner");
        var interceptor = new IdentityInterceptor(new ProbeLog());
        var interceptScope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(interceptor), advice));
        var state = new ProbeState();
        Check(ReferenceEquals(state, interceptScope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>()
            .Resolve(interceptor).Transform(state, null, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)),
            "interception keeps its exact state result through a wrapper");
        Check(probe.Work.SequenceEqual(new[] { 1, 2, 0, 0, 0, 0 }) && probe.SuccessCalls == 6,
            "a wrapper reads declared candidate work only where the operation supports it");
    }

    [Fact]
    public void Duration()
    {
        var clock = new ManualClock();
        var duration = new DurationAccumulator();
        var probe = new Probe { ChildAction = () => clock.Advance(17) };
        var source = new ProbeMutation(probe);
        var operation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new DurationAdvice(duration, clock)))).Resolve(source);
        InvokeOperation(operation);
        Check(duration.CurrentDuration.Ticks == 17 && clock.Reads == 2, "duration reads the supplied TimeProvider at entry and cleanup");
        probe.ChildFailure = new InvalidOperationException("timed failure");
        Check(ReferenceEquals(Capture(() => InvokeOperation(operation)), probe.ChildFailure) && duration.CurrentDuration.Ticks == 34,
            "failed work is included in elapsed duration");
        probe.ChildFailure = new OperationCanceledException("timed cancellation");
        Check(ReferenceEquals(Capture(() => InvokeOperation(operation)), probe.ChildFailure) && duration.CurrentDuration.Ticks == 51,
            "normal cancellation retains its exception and contributes duration during cleanup");

        var timestampFailure = new InvalidOperationException("timestamp failure");
        clock.FailOnRead = clock.Reads + 1;
        clock.Failure = timestampFailure;
        var before = probe.ChildCalls;
        Check(ReferenceEquals(Capture(() => InvokeOperation(operation)), timestampFailure) && probe.ChildCalls == before,
            "a failing entry timestamp does not run the child");
        clock.FailOnRead = clock.Reads + 2;
        Check(ReferenceEquals(Capture(() => InvokeOperation(operation)), timestampFailure),
            "a failing cleanup timestamp follows ordinary finally semantics");
    }

    [Fact]
    public void TimingOrder()
    {
        foreach (var timingInside in new[] { false, true })
        {
            var clock = new ManualClock();
            var duration = new DurationAccumulator();
            var source = new ProbeMutation(new Probe { ChildAction = () => clock.Advance(17) });
            var timing = Aspect.Create(Pointcut.Reference(source), new DurationAdvice(duration, clock));
            var observing = Aspect.Create(Pointcut.Reference(source), new CountCalls(new CallCounter(),
                new AdviceLog { AfterSuccess = () => clock.Advance(5) }));
            var aspects = timingInside ? new[] { timing, observing } : new[] { observing, timing };
            InvokeOperation(Typed(ResolutionScope.Create(aspects)).Resolve(source));
            Check(duration.CurrentDuration.Ticks == (timingInside ? 17 : 22),
                "application order determines whether observer callbacks are inside the timed boundary");
        }
    }

    private sealed class Probe
    {
        public int Entries { get; set; }
        public int ChildCalls { get; set; }
        public int SuccessCalls { get; set; }
        public Exception? EntryFailure { get; set; }
        public Exception? ChildFailure { get; set; }
        public Action? ChildAction { get; set; }
        public List<string> Events { get; } = [];
        public List<int> Work { get; } = [];
    }

    private readonly struct LifecycleWrapper(Probe probe, bool countsWork) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            var id = ++probe.Entries;
            probe.Events.Add($"enter:{id}");
            if (probe.EntryFailure is { } entryFailure)
                ThrowFailure(entryFailure);
            try
            {
                var result = invocation.Proceed();
                probe.Events.Add($"success:{id}");
                probe.SuccessCalls++;
                probe.Work.Add(countsWork ? invocation.CountCandidateWork(result) : 0);
                return result;
            }
            catch (Exception)
            {
                probe.Events.Add($"fault:{id}");
                throw;
            }
            finally
            {
                probe.Events.Add($"finally:{id}");
            }
        }
    }

    private sealed class LifecycleAdvice(Probe probe) : OperatorAdvice
    {
        public int Wraps { get; private set; }

        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source)
        {
            Wraps++;
            return operation.Wrap(executor, new LifecycleWrapper(probe, operation.SupportsCandidateWork));
        }
    }

    private sealed record ProbeMutation(Probe Probe) : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
        {
            Probe.ChildCalls++;
            Probe.Events.Add("child");
            Probe.ChildAction?.Invoke();
            if (Probe.ChildFailure is { } failure)
                ThrowFailure(failure);
            return parents;
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private long now;
        public int Reads { get; private set; }
        public int FailOnRead { get; set; } = -1;
        public Exception Failure { get; set; } = new InvalidOperationException("clock failure");
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public void Advance(long ticks) => now += ticks;
        public override long GetTimestamp()
        {
            Reads++;
            if (Reads == FailOnRead)
                ThrowFailure(Failure);
            return now;
        }
    }
}
