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
        { action(); return null; }
        catch (Exception failure) { return failure; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFailure(Exception failure) => throw failure;

    private static ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Typed(ResolutionScope scope) =>
        scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

    private static void Invoke(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> operation) =>
        operation.Mutate(Input, RandomNumberGenerator.Create(4), Problem.SearchSpace, Problem);

    [Fact]
    public void FailureMatrix()
    {
        foreach (var childKind in new[] { "success", "failure", "cancellation" })
            foreach (var failCallback in new[] { false, true })
                foreach (var failCleanup in new[] { false, true })
                {
                    var probe = new Probe
                    {
                        ChildFailure = childKind switch
                        {
                            "failure" => new InvalidOperationException("child failure"),
                            "cancellation" => new OperationCanceledException("child cancellation"),
                            _ => null
                        },
                        CallbackFailure = failCallback ? new InvalidOperationException("callback failure") : null,
                        CleanupFailure = failCleanup ? new InvalidOperationException("cleanup failure") : null
                    };
                    var source = new ProbeMutation(probe);
                    var operation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new LifecycleAdvice(probe)))).Resolve(source);
                    var failure = Capture(() => Invoke(operation));
                    var expected = new[] { probe.ChildFailure, probe.CallbackFailure, probe.CleanupFailure }.OfType<Exception>().ToArray();
                    if (expected.Length == 0)
                        Check(failure is null, "a successful lifecycle returns normally");
                    else if (expected.Length == 1)
                        Check(ReferenceEquals(failure, expected[0]), "a lone child/callback/cleanup failure propagates unchanged");
                    else
                        Check(failure is AggregateException aggregate && aggregate.InnerExceptions.SequenceEqual(expected),
                            "aggregate preserves child, callback and cleanup failures in occurrence order");
                    Check(probe.Events.SequenceEqual(new[] { "enter:1", "child", probe.ChildFailure is null ? "success:1" : "fault:1", "finally:1" }),
                        "success callback failure is not a child fault and cleanup runs exactly once");
                    Check(expected.All(item => item.StackTrace?.Contains(nameof(ThrowFailure), StringComparison.Ordinal) == true),
                        "original throw-site stacks survive callback capture and cleanup");
                    Check(probe.ChildCalls == 1 && probe.SuccessCalls == (probe.ChildFailure is null ? 1 : 0),
                        "completed success updates remain visible and no child work is repeated");
                }

        var entryFailure = new InvalidOperationException("enter failure");
        var entry = new Probe { EntryFailure = entryFailure };
        var entrySource = new ProbeMutation(entry);
        var entryOperation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(entrySource), new LifecycleAdvice(entry)))).Resolve(entrySource);
        Check(ReferenceEquals(Capture(() => Invoke(entryOperation)), entryFailure) && entry.Events.SequenceEqual(new[] { "enter:1" }) && entry.ChildCalls == 0,
            "failed Enter creates no token and invokes neither child, Fault nor Finally");

        var nestedFailure = new AggregateException(new InvalidOperationException("inner a"), new InvalidOperationException("inner b"));
        var nested = new Probe { ChildFailure = nestedFailure, CallbackFailure = new InvalidOperationException("fault callback") };
        var nestedSource = new ProbeMutation(nested);
        var nestedOperation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(nestedSource), new LifecycleAdvice(nested)))).Resolve(nestedSource);
        Check(Capture(() => Invoke(nestedOperation)) is AggregateException outer && outer.InnerExceptions.Count == 2 &&
            ReferenceEquals(outer.InnerExceptions[0], nestedFailure), "an existing aggregate child failure is preserved without flattening");
    }

    [Fact]
    public void NestedTokens()
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
            Invoke(operation);
        };
        Invoke(operation);
        Check(probe.Events.SequenceEqual(new[] { "enter:1", "child", "enter:2", "child", "success:2", "finally:2", "success:1", "finally:1" }),
            "nested calls preserve independent outer and inner tokens");
    }

    [Fact]
    public void OperationsAndViews()
    {
        var probe = new Probe();
        var advice = new LifecycleAdvice(probe);
        var source = new SharedOperations(new ProbeLog());
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), advice));
        var mutation = Typed(scope).Resolve((IOperator<RealVector, Mutation>)source, out ILiveValue? control);
        control!.Value = 14;
        Check(mutation.Mutate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)[0][0] == 14,
            "lifecycle wrappers preserve the live state used by the callable operation");
        _ = Typed(scope).Resolve((IOperator<RealVector, Evaluation>)source).Evaluate([Input[0], Input[0]],
            RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);
        Check(Typed(scope).Resolve((IOperator<RealVector, Checksum>)source).Read() == 14, "an external scalar adapter uses the shared lifecycle protocol");
        var broad = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var narrow = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        _ = broad.Resolve((IOperator<RealVector, Termination>)source).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
        _ = narrow.Resolve(((IOperator<RealVector, Termination>)source).Named("term")).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
        Check(probe.Preparations == 1 && probe.Bindings == 5 && source.Log.Preparations == 1,
            "general lifecycle preparation is shared while operation/state constructors remain separate");
        var interceptor = new IdentityInterceptor(new ProbeLog());
        var interceptScope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(interceptor), advice));
        var state = new ProbeState();
        Check(ReferenceEquals(state, interceptScope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>()
            .Resolve(interceptor).Transform(state, null, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)),
            "interception keeps its exact state result through lifecycle advice");
        Check(probe.Work.SequenceEqual(new[] { 1, 2, 0, 0, 0, 0 }) && probe.SuccessCalls == 6,
            "lifecycle success receives the operation's declared work without manufacturing work for scalar/state operations");
    }

    [Fact]
    public void Duration()
    {
        var clock = new ManualClock();
        var duration = new DurationAccumulator();
        var probe = new Probe { ChildAction = () => clock.Advance(17) };
        var source = new ProbeMutation(probe);
        var operation = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new DurationAdvice(duration, clock)))).Resolve(source);
        Invoke(operation);
        Check(duration.CurrentDuration.Ticks == 17 && clock.Reads == 2, "duration uses a long timestamp token and the supplied TimeProvider");
        probe.ChildFailure = new InvalidOperationException("timed failure");
        Check(ReferenceEquals(Capture(() => Invoke(operation)), probe.ChildFailure) && duration.CurrentDuration.Ticks == 34,
            "failed work is included in elapsed duration");
        probe.ChildFailure = new OperationCanceledException("timed cancellation");
        Check(ReferenceEquals(Capture(() => Invoke(operation)), probe.ChildFailure) && duration.CurrentDuration.Ticks == 51,
            "normal cancellation retains its exception and contributes duration during cleanup");

        var timestampFailure = new InvalidOperationException("timestamp failure");
        clock.FailOnRead = clock.Reads + 1;
        clock.Failure = timestampFailure;
        var before = probe.ChildCalls;
        Check(ReferenceEquals(Capture(() => Invoke(operation)), timestampFailure) && probe.ChildCalls == before,
            "a failing duration Enter does not run the child");
        clock.FailOnRead = clock.Reads + 2;
        var child = probe.ChildFailure;
        Check(Capture(() => Invoke(operation)) is AggregateException aggregate && aggregate.InnerExceptions.Count == 2 &&
            ReferenceEquals(aggregate.InnerExceptions[0], child) && ReferenceEquals(aggregate.InnerExceptions[1], timestampFailure),
            "duration cleanup failure is aggregated after the child cancellation");
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
            Invoke(Typed(ResolutionScope.Create(aspects)).Resolve(source));
            Check(duration.CurrentDuration.Ticks == (timingInside ? 17 : 22),
                "application order determines whether observer callbacks are inside the timed boundary");
        }
    }

    private sealed class Probe
    {
        public int Preparations { get; set; }
        public int Bindings { get; set; }
        public int Entries { get; set; }
        public int ChildCalls { get; set; }
        public int SuccessCalls { get; set; }
        public Exception? EntryFailure { get; set; }
        public Exception? ChildFailure { get; set; }
        public Exception? CallbackFailure { get; set; }
        public Exception? CleanupFailure { get; set; }
        public Action? ChildAction { get; set; }
        public List<string> Events { get; } = [];
        public List<int> Work { get; } = [];
    }

    private readonly record struct Token(int Id);

    private readonly struct Hooks(Probe probe) : IOperationCallCallbacks<Token>
    {
        public Token OnStarting()
        {
            var id = ++probe.Entries;
            probe.Events.Add($"enter:{id}");
            if (probe.EntryFailure is { } failure)
                ThrowFailure(failure);
            return new Token(id);
        }
        public void OnSucceeded(Token token, int candidateWork)
        {
            probe.Events.Add($"success:{token.Id}");
            probe.SuccessCalls++;
            probe.Work.Add(candidateWork);
            if (probe.CallbackFailure is { } failure)
                ThrowFailure(failure);
        }
        public void OnFailed(Token token, Exception failure)
        {
            probe.Events.Add($"fault:{token.Id}");
            if (probe.CallbackFailure is { } callback)
                ThrowFailure(callback);
        }
        public void OnFinished(Token token)
        {
            probe.Events.Add($"finally:{token.Id}");
            if (probe.CleanupFailure is { } failure)
                ThrowFailure(failure);
        }
    }

    private sealed class LifecycleAdvice(Probe probe) : OperatorAdvice
    {
        public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source)
        {
            probe.Preparations++;
            return new WrapperFactory(probe);
        }
        private sealed class WrapperFactory(Probe probe) : OperatorAdviceWrapperFactory
        {
            public override WrapperExecutorFactory GetWrapperFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
                IOperator<TCandidate, TOperation> source,
                OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter)
            {
                probe.Bindings++;
                return adapter.Hooks<Hooks, Token>(new Hooks(probe));
            }
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
