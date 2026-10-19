using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures.Authoring;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class OperatorAdviceTests
{
    private static void Check(bool condition, string description) => Assert.True(condition, description);

    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));
    private static readonly RealVector[] Input = [new(2.0)];

    private static void Reject(Action action, string expected)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            Check(exception.Message.Contains(expected, StringComparison.Ordinal), $"diagnostic includes '{expected}'");
            return;
        }
        throw new InvalidOperationException($"Expected advice rejection: {expected}");
    }

    private static void RejectSame(Action action, Exception expected)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Check(ReferenceEquals(exception, expected), "single invocation failure propagates unchanged");
            return;
        }
        throw new InvalidOperationException("Expected the original invocation failure.");
    }

    private static ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Typed(ResolutionScope scope) =>
        scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

    private static double Invoke(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> operation) =>
        operation.Mutate(Input, RandomNumberGenerator.Create(17), Problem.SearchSpace, Problem)[0][0];

    [Fact]
    public void MultipleOperations()
    {
        foreach (var evaluationFirst in new[] { false, true })
        {
            var source = new SharedOperations(new ProbeLog());
            IOperator<Mutation, RealVector> mutation = source;
            IOperator<Evaluation, RealVector> evaluation = source;
            var generalTotal = new CallCounter();
            var generalLog = new AdviceLog();
            var typedTotal = new CallCounter();
            var typedLog = new AdviceLog();
            var general = Aspect.Create(Pointcut.Reference(source), new CountCalls(generalTotal, generalLog));
            var typedAdvice = Aspect.Create(Pointcut.Reference(mutation), new CountCalls<Mutation, RealVector>(typedTotal, typedLog));
            var root = ResolutionScope.Create(general, typedAdvice);
            var scope = Typed(root);
            if (evaluationFirst)
            {
                _ = scope.Resolve(evaluation).Evaluate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);
                Check(typedLog.Checks == 0 && typedLog.Wraps == 0, "mutation advice is not applied to the evaluation view of the same object");
            }
            _ = Invoke(scope.Resolve(mutation));
            if (!evaluationFirst)
                _ = scope.Resolve(evaluation).Evaluate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);
            Check(scope.Resolve((IOperator<Checksum, RealVector>)source).Read(RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem) == 3, "general advice dispatches to an external scalar adapter");
            var term = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
            _ = term.Resolve((IOperator<Termination, RealVector>)source).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            Check(generalTotal.Value == 4 && typedTotal.Value == 1, "general counts all operations; typed counts only mutation");
            Check(generalLog.Checks == 4 && generalLog.Wraps == 4 && typedLog.Checks == 1 && typedLog.Wraps == 1,
                "each resolution checks and wraps its own operation view once");
            Check(generalLog.CheckedSources.All(item => ReferenceEquals(item.Target, source)) &&
                generalLog.WrappedSources.All(item => ReferenceEquals(item.Target, source)) &&
                typedLog.WrappedSources.All(item => ReferenceEquals(item.Target, source)), "check and wrap receive the selected source reference");
            _ = Invoke(Typed(root.With()).Resolve(mutation));
            Check(generalLog.Wraps == 5 && typedLog.Wraps == 2 && generalTotal.Value == 5 && typedTotal.Value == 2,
                "contextual reconstruction wraps the new executor with the same advice objects");
            _ = Invoke(Typed(ResolutionScope.Create(general, typedAdvice)).Resolve(mutation));
            Check(source.Log.Preparations == 2 && generalTotal.Value == 6,
                "independent roots prepare independent operator state while advice data stays on the advice");
        }
    }

    [Fact]
    public void StateViews()
    {
        foreach (var narrowFirst in new[] { false, true })
        {
            var source = new SharedOperations(new ProbeLog());
            IOperator<Termination, RealVector> term = source;
            var named = term.Named("state").Named("outer");
            var generalLog = new AdviceLog();
            var typedLog = new AdviceLog();
            var skippedLog = new AdviceLog { FailCheck = true };
            var total = new CallCounter();
            var root = ResolutionScope.Create(
                Aspect.Create(Pointcut.Reference(source), new CountCalls(total, generalLog)),
                Aspect.Create(Pointcut.Reference(term), new CountCalls<Termination, RealVector>(new CallCounter(), typedLog)),
                Aspect.Create(Pointcut.Reference((IOperator<Mutation, RealVector>)source),
                    new CountCalls<Mutation, RealVector>(new CallCounter(), skippedLog)));
            var broad = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
            var narrow = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
            if (narrowFirst)
                _ = narrow.Resolve(named).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            _ = broad.Resolve(named).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            if (!narrowFirst)
                _ = narrow.Resolve(named).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            var other = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, OtherState>();
            _ = other.Resolve(named).IsTerminalState(new OtherState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            Check(source.Log.Preparations == 1, "three compatible state views prepare the operator once in either resolution order");
            Check(generalLog.Wraps == 3 && typedLog.Wraps == 3 && total.Value == 3 && skippedLog.Checks == 0,
                "state views are wrapped separately without applying another operation's advice");
            var advised = narrow.Resolve(named, out ILiveValue? control);
            control!.Value = 12;
            Check(advised.IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem), "an advised terminator retains its live control");
        }
        var interceptor = new IdentityInterceptor(new ProbeLog());
        var interceptTotal = new CallCounter();
        var interceptLog = new AdviceLog();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(interceptor), new CountCalls(interceptTotal, interceptLog)));
        var exact = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        var state = new ProbeState();
        Check(ReferenceEquals(state, exact.Resolve(interceptor).Transform(state, null, RandomNumberGenerator.Create(1),
            Problem.SearchSpace, Problem)) && interceptTotal.Value == 1, "the same general counter works on interception without altering its result");
        Reject(() => scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>().Resolve(interceptor), "cannot provide");
        Check(interceptor.Log.Constructions == 1 && interceptLog.Wraps == 1,
            "an incompatible raw view neither constructs another node nor wraps one");
    }

    [Fact]
    public void Ordering()
    {
        IOperator<Mutation, RealVector> source = new IdentityMutation();
        var order = new List<string>();
        var total = new CallCounter();
        var localLog = new AdviceLog { AfterSuccess = () => order.Add("local") };
        var local = new CountCalls(total, localLog);
        var observedCount = 0;
        var observer = new CountCalls(new CallCounter(), new AdviceLog
        {
            AfterSuccess = () =>
        {
            order.Add("observer");
            observedCount = total.Value;
        }
        });
        var parentA = new CountCalls(new CallCounter(), new AdviceLog { AfterSuccess = () => order.Add("parent-a") });
        var parentB = new CountCalls(new CallCounter(), new AdviceLog { AfterSuccess = () => order.Add("parent-b") });
        var parent = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), parentA), Aspect.Create(Pointcut.Reference(source), parentB));
        var scope = parent.With(new Pairs((Pointcut.Reference(source), local), (Pointcut.Reference(source), observer),
            (Pointcut.Reference(source), local)));
        _ = Invoke(Typed(scope).Resolve(source));
        Check(order.SequenceEqual(new[] { "local", "observer", "parent-a", "parent-b" }),
            "successful callbacks run in current-layer, aspect and pair application order");
        Check(observedCount == 1 && total.Value == 1 && localLog.Wraps == 1,
            "a duplicate advice neither counts twice nor moves its first position behind observation");

        var skipped = new AdviceLog { FailCheck = true };
        var duplicate = new CountCalls(new CallCounter(), skipped);
        var firstMiss = ResolutionScope.Create(new Pairs((Pointcut.Predicate<IOperator>(_ => false), duplicate),
            (Pointcut.Reference(source), duplicate)));
        _ = Invoke(Typed(firstMiss).Resolve(source));
        Check(skipped.Checks == 0, "first-position deduplication precedes matching, as in the shipping resolver");

        var sharedResult = new CallCounter();
        var first = new AdviceLog();
        var second = new AdviceLog();
        _ = Invoke(Typed(ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(source), new CountCalls(sharedResult, first)),
            Aspect.Create(Pointcut.Reference(source), new CountCalls(sharedResult, second)))).Resolve(source));
        Check(sharedResult.Value == 2 && first.Wraps == 1 && second.Wraps == 1,
            "distinct advice instances remain distinct even when their type and result accumulator match");
    }

    [Fact]
    public void ControlsAndContexts()
    {
        var source = new SharedOperations(new ProbeLog());
        IOperator<Mutation, RealVector> mutation = source;
        var inner = mutation.Named("inner");
        var outer = inner.Named("outer");
        var root = ResolutionScope.Create();
        var raw = Typed(root).Resolve(mutation, out ILiveValue? rawControl);
        var count = new CallCounter();
        var log = new AdviceLog();
        var scope = Typed(root.With(Aspect.Create(Pointcut.OfType<IOperator>(), new CountCalls(count, log))));
        var advised = scope.Resolve(outer, out ILiveValue? control);
        Check(ReferenceEquals(rawControl, control) && advised is not ILiveValue,
            "control lookup follows names and non-control inspection wrappers to the source");
        control!.Value = 17;
        Check(Invoke(advised) == 17 && count.Value == 3, "calls use the advised chain and count each of three selected configuration boundaries");
        Check(Invoke(raw) == 17 && count.Value == 3, "earlier raw bindings retain their original observation context");
        var rebound = scope.Resolve(outer, out ILiveValue? reboundControl);
        Check(ReferenceEquals(control, reboundControl) && Invoke(rebound) == 17 && count.Value == 6,
            "rebuilding the chain neither resets controls nor loses counting");
        Check(log.Wraps == 6 && new IOperator[] { source, inner, outer }.All(selected =>
            log.WrappedSources.Count(item => ReferenceEquals(item.Target, selected)) == 2),
            "each resolution wraps every selected configuration with its own source identity");

        IOperator<Mutation, RealVector> child = new CountingMutation();
        var composite = new ContextualMutation(child);
        var originalScope = ResolutionScope.Create();
        var original = Typed(originalScope).Resolve(composite);
        var childCounter = new CallCounter();
        var childLog = new AdviceLog();
        var contextual = Typed(originalScope.With(Aspect.Create(Pointcut.Reference(child), new CountCalls(childCounter, childLog)))).Resolve(composite);
        Check(Invoke(original) == 1 && Invoke(contextual) == 2 && Invoke(original) == 3 && childCounter.Value == 1,
            "newly resolved children acquire requesting advice while original parent nodes keep their old children");
        Check(Invoke(contextual) == 4 && childCounter.Value == 2 && childLog.Wraps == 1,
            "prepared child execution state survives contextual reconstruction");
    }

    [Fact]
    public void Ownership()
    {
        foreach (var policy in new[] { ChildScopePolicy.Fresh, ChildScopePolicy.Retained, ChildScopePolicy.Shared })
        {
            IOperator<Mutation, RealVector> source = new CountingMutation();
            var log = new AdviceLog();
            var total = new CallCounter();
            var aspect = Aspect.Create(Pointcut.Reference(source), new CountCalls(total, log));
            var root = ResolutionScope.Create(aspect);
            var selector = new ChildScopeSelector(policy);
            var first = Typed(selector.GetScopeFor(source, root)).Resolve(source);
            var second = Typed(selector.GetScopeFor(source, root.With())).Resolve(source);
            Check(Invoke(first) == 1 && Invoke(second) == (policy == ChildScopePolicy.Fresh ? 1 : 2), $"{policy} preserves operator state semantics under advice");
            Check(total.Value == 2 && log.Wraps == 2, $"{policy} applies the advice to every activation");
        }
        var references = new AdviceLog();
        var referenceScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.OfType<IOperator>(), new CountCalls(new CallCounter(), references))));
        IOperator<Mutation, RealVector> firstRecord = new CountingMutation();
        IOperator<Mutation, RealVector> secondRecord = new CountingMutation();
        Check(Invoke(referenceScope.Resolve(firstRecord)) == 1 && Invoke(referenceScope.Resolve(secondRecord)) == 1 &&
            references.WrappedSources.Select(item => item.Target).SequenceEqual(new object[] { firstRecord, secondRecord }),
            "value-equal configuration records remain separate sources with separate operator state");
    }

    [Fact]
    public void BindingFailures()
    {
        var child = new SharedOperations(new ProbeLog());
        var source = new ContextualMutation(child);
        var log = new AdviceLog { FailCheck = true };
        var count = new CallCounter();
        var scope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CountCalls(count, log))));
        Reject(() => scope.Resolve(source), "advice check failure");
        Check(child.Log.Preparations == 0 && child.Log.Constructions == 0, "a failed advice check precedes the raw child graph");
        log.FailCheck = false;
        log.FailWrap = true;
        Reject(() => scope.Resolve(source), "advice wrapping failure");
        Check(child.Log.Preparations == 1 && child.Log.Constructions == 1, "wrapping follows raw construction");
        log.FailWrap = false;
        Check(Invoke(scope.Resolve(source)) == 3 && log.Checks == 3 && log.Wraps == 2 && count.Value == 1 && child.Log.Preparations == 1,
            "failed advice is not cached and a retry reuses the prepared child");

        var wrongChild = new SharedOperations(new ProbeLog());
        var wrongSource = new ContextualMutation(wrongChild);
        var wrongLog = new AdviceLog();
        var wrong = new WrongViewAdvice(new CallCounter(), wrongLog);
        var wrongScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(wrongSource), wrong)));
        var sentinel = new ControlSentinel();
        ILiveValue? control = sentinel;
        Reject(() => wrongScope.Resolve(wrongSource, out control), "cannot provide");
        Check(ReferenceEquals(control, sentinel), "advice returning another executor type exposes no controls");
        wrong.ReturnWrongView = false;
        _ = Invoke(wrongScope.Resolve(wrongSource));
        Check(wrongLog.Wraps == 2 && wrongChild.Log.Preparations == 1, "a rejected advice result is not cached and the prepared child is reused");

        var termSource = new SharedOperations(new ProbeLog());
        IOperator<Termination, RealVector> term = termSource;
        var generalLog = new AdviceLog();
        var typedLog = new AdviceLog();
        var generalTotal = new CallCounter();
        var typedTotal = new CallCounter();
        var root = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(term), new CountCalls(generalTotal, generalLog)),
            Aspect.Create(Pointcut.Reference(term), new CountCalls<Termination, RealVector>(typedTotal, typedLog)));
        var broad = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var narrow = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        var first = broad.Resolve(term);
        typedLog.FailCheck = true;
        Reject(() => narrow.Resolve(term), "advice check failure");
        Check(termSource.Log.Constructions == 1 && generalLog.Wraps == 1, "a failed check in a second view invokes neither the raw constructor nor any wrapping");
        _ = first.IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
        Check(generalTotal.Value == 1 && typedTotal.Value == 1, "a failed second view leaves the first executor usable");
        typedLog.FailCheck = false;
        _ = narrow.Resolve(term).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
        Check(termSource.Log.Preparations == 1 && generalTotal.Value == 2 && typedTotal.Value == 2,
            "retry reuses the prepared operator across views");

        var unsupported = new UnsupportedAdvice();
        var unsupportedScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), unsupported)));
        Check(!unsupportedScope.TryResolve(source, out var absent, out var reason) && absent is null &&
            reason.Contains(typeof(UnsupportedAdvice).FullName!, StringComparison.Ordinal) &&
            reason.Contains(nameof(Mutation), StringComparison.Ordinal) && reason.Contains("Supported extension bases", StringComparison.Ordinal),
            "unsupported Advice reports its type, operation and supported bases without leaking an invalid cast");
    }

    [Fact]
    public void InvocationFailures()
    {
        var source = new SharedOperations(new ProbeLog { FailConstruction = true });
        var log = new AdviceLog();
        var counter = new CallCounter();
        var scope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CountCalls(counter, log))));
        Reject(() => scope.Resolve((IOperator<Mutation, RealVector>)source), "construction failure");
        Check(log.Checks == 1 && log.Wraps == 0, "advice checks precede raw construction; wrapping follows it");
        source.Log.FailConstruction = false;
        _ = Invoke(scope.Resolve((IOperator<Mutation, RealVector>)source));
        Check(log.Wraps == 1 && counter.Value == 1 && source.Log.Preparations == 1,
            "raw construction failure preserves the prepared operator");

        var construction = new ConstructionFailureAdvice();
        var constructionSource = new SharedOperations(new ProbeLog());
        var constructionScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(constructionSource), construction)));
        var sentinel = new ControlSentinel();
        ILiveValue? control = sentinel;
        RejectSame(() => constructionScope.Resolve((IOperator<Mutation, RealVector>)constructionSource, out control), construction.Failure);
        Check(ReferenceEquals(control, sentinel) && constructionSource.Log.Constructions == 1,
            "a wrapper constructor failure exposes no controls even though the raw node was constructed");
        construction.Fail = false;
        _ = constructionScope.Resolve((IOperator<Mutation, RealVector>)constructionSource, out control);
        Check(!ReferenceEquals(control, sentinel) && construction.Wraps == 2 && constructionSource.Log.Preparations == 1,
            "wrapper construction retries over the existing prepared owner");

        var invocation = new InvocationLog();
        IOperator<Mutation, RealVector> throwing = new ThrowingMutation(invocation);
        var inner = new CallCounter();
        var outer = new CallCounter();
        var outerLog = new AdviceLog();
        var operation = Typed(ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(throwing), new CountCalls(inner, new AdviceLog())),
            Aspect.Create(Pointcut.Reference(throwing), new CountCalls(outer, outerLog)))).Resolve(throwing);
        _ = Invoke(operation);
        invocation.Failure = new OperationCanceledException("fixture cancellation");
        RejectSame(() => Invoke(operation), invocation.Failure);
        invocation.Failure = new InvalidOperationException("fixture child failure");
        RejectSame(() => Invoke(operation), invocation.Failure);
        Check(inner.Value == 1 && outer.Value == 1, "failed and canceled child calls do not count as successes");
        invocation.Failure = null;
        var callbackFailure = new InvalidOperationException("fixture callback failure");
        outerLog.AfterSuccess = () => throw callbackFailure;
        RejectSame(() => Invoke(operation), callbackFailure);
        Check(inner.Value == 2 && outer.Value == 2, "a wrapper failure after success leaves completed inner updates intact");
        outerLog.AfterSuccess = null;
        _ = Invoke(operation);
        Check(inner.Value == 3 && outer.Value == 3 && invocation.Calls == 5, "later calls remain usable without extra child invocations");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DiscardAdvice(ResolutionScope root, IOperator<Mutation, RealVector> source)
    {
        var advice = new CountCalls(new CallCounter(), new AdviceLog());
        _ = Typed(root.With(Aspect.Create(Pointcut.Reference(source), advice))).Resolve(source);
        return new WeakReference(advice);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Source, WeakReference Execution) DiscardFreshState(ResolutionScope root, Aspect aspect)
    {
        IOperator<Mutation, RealVector> source = new CountingMutation();
        var executor = Typed(root.CreateChildScope(aspect)).Resolve(source);
        return (new WeakReference(source), new WeakReference(executor));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Executor,
        WeakReference Advice, CallCounter Total) KeepExecution(ResolutionScope root, IOperator<Mutation, RealVector> source)
    {
        var counter = new CallCounter();
        var advice = new CountCalls(counter, new AdviceLog());
        var executor = Typed(root.With(Aspect.Create(Pointcut.Reference(source), advice))).Resolve(source);
        return (executor, new WeakReference(advice), counter);
    }

    [Fact]
    public void Lifetime()
    {
        var root = ResolutionScope.Create();
        IOperator<Mutation, RealVector> source = new CountingMutation();
        var discardedAdvice = DiscardAdvice(root, source);
        var advice = new CountCalls(new CallCounter(), new AdviceLog());
        var aspect = Aspect.Create(Pointcut.OfType<IOperator>(), advice);
        var discardedFresh = DiscardFreshState(root, aspect);
        var live = KeepExecution(root, source);
        for (var attempt = 0; attempt < 5 && (discardedAdvice.IsAlive || discardedFresh.Source.IsAlive ||
            discardedFresh.Execution.IsAlive || live.Advice.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Check(!discardedAdvice.IsAlive, "a live owner does not retain discarded advice");
        Check(!discardedFresh.Source.IsAlive && !discardedFresh.Execution.IsAlive,
            "a long-lived advice/aspect does not retain discarded fresh owners or sources");
        Check(Invoke(live.Executor) == 1 && live.Total.Value == 1, "a live advised execution retains the counter it still needs");
        Check(!live.Advice.IsAlive, "an advised executor keeps its wrapper data, not the advice object");
        GC.KeepAlive(root);
        GC.KeepAlive(source);
        GC.KeepAlive(advice);
        GC.KeepAlive(aspect);
    }

    private sealed class Pairs : Aspect
    {
        public Pairs(params (Pointcut Pointcut, Advice Advice)[] pairs)
        {
            Advices = [.. pairs];
        }
    }

    private sealed class ControlSentinel : ILiveValue
    {
        public int Value { get; set; } = -1;
    }

    private sealed class UnsupportedAdvice : Advice<IOperator<Mutation, RealVector>>;

    private sealed class InvocationLog
    {
        public int Calls { get; set; }
        public Exception? Failure { get; set; }
    }

    private sealed record ThrowingMutation(InvocationLog Log) : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
        {
            Log.Calls++;
            if (Log.Failure is { } failure)
                throw failure;
            return parents;
        }
    }

    private sealed class ConstructionFailureAdvice : MutatorAdvice<RealVector>
    {
        public bool Fail { get; set; } = true;
        public int Wraps { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("fixture wrapper constructor failure");

        protected override IMutationExecutor<RealVector, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
            IMutationExecutor<RealVector, TSearchSpace, TProblem> executor, IOperator<Mutation, RealVector> mutator)
        {
            Wraps++;
            return Fail ? throw Failure : executor;
        }
    }
}
