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
        { action(); }
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
        { action(); }
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
            IOperator<RealVector, Mutation> mutation = source;
            IOperator<RealVector, Evaluation> evaluation = source;
            var generalTotal = new CallCounter();
            var generalLog = new AdviceLog();
            var typedTotal = new CallCounter();
            var typedLog = new AdviceLog();
            var general = Aspect.Create(Pointcut.Reference(source), new CountCalls(generalTotal, generalLog));
            var typedAdvice = Aspect.Create(Pointcut.Reference(mutation), new CountCalls<RealVector, Mutation>(typedTotal, typedLog));
            var root = ResolutionScope.Create(general, typedAdvice);
            var scope = Typed(root);
            if (evaluationFirst)
            {
                _ = scope.Resolve(evaluation).Evaluate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);
                Check(typedLog.Preparations == 0, "mutation advice is not prepared for the evaluation view of the same object");
            }
            _ = Invoke(scope.Resolve(mutation));
            if (!evaluationFirst)
                _ = scope.Resolve(evaluation).Evaluate(Input, RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);
            Check(scope.Resolve((IOperator<RealVector, Checksum>)source).Read() == 3, "general advice dispatches to an external scalar adapter");
            var term = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
            _ = term.Resolve((IOperator<RealVector, Termination>)source).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
            Check(generalTotal.Value == 4 && typedTotal.Value == 1, "general counts all operations; typed counts only mutation");
            Check(generalLog.Preparations == 1 && generalLog.Bindings == 4 && generalLog.StateAt(0).Calls == 4,
                "one general prepared state has four operation-specific wrapper constructors");
            Check(typedLog.Preparations == 1 && typedLog.Bindings == 1, "one typed prepared state belongs to its operation view");
            Check(generalLog.PreparedSources.All(item => ReferenceEquals(item.Target, source)) &&
                generalLog.BoundSources.All(item => ReferenceEquals(item.Target, source)) &&
                typedLog.PreparedSources.All(item => ReferenceEquals(item.Target, source)), "prepare and bind receive the selected source reference");
            _ = Invoke(Typed(root.With()).Resolve(mutation));
            Check(generalLog.Bindings == 4 && typedLog.Bindings == 1 && generalTotal.Value == 5 && typedTotal.Value == 2,
                "contextual reconstruction reuses exact wrapper factories and persistent advice state");
            _ = Invoke(Typed(ResolutionScope.Create(general, typedAdvice)).Resolve(mutation));
            Check(source.Log.Preparations == 2 && generalLog.Preparations == 2 && typedLog.Preparations == 2 &&
                generalLog.StateAt(1).Calls == 1, "independent roots prepare independent operator and advice state");
        }
    }

    [Fact]
    public void StateViews()
    {
        foreach (var narrowFirst in new[] { false, true })
        {
            var source = new SharedOperations(new ProbeLog());
            IOperator<RealVector, Termination> term = source;
            var named = term.Named("state").Named("outer");
            var generalLog = new AdviceLog();
            var typedLog = new AdviceLog();
            var skippedLog = new AdviceLog { FailPreparation = true };
            var total = new CallCounter();
            var root = ResolutionScope.Create(
                Aspect.Create(Pointcut.Reference(source), new CountCalls(total, generalLog)),
                Aspect.Create(Pointcut.Reference(term), new CountCalls<RealVector, Termination>(new CallCounter(), typedLog)),
                Aspect.Create(Pointcut.Reference((IOperator<RealVector, Mutation>)source),
                    new CountCalls<RealVector, Mutation>(new CallCounter(), skippedLog)));
            var broad = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
            var narrow = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
            if (narrowFirst)
                _ = narrow.Resolve(named).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
            _ = broad.Resolve(named).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
            if (!narrowFirst)
                _ = narrow.Resolve(named).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
            var other = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, OtherState>();
            _ = other.Resolve(named).IsTerminalState(new OtherState(), Problem.SearchSpace, Problem);
            Check(source.Log.Preparations == 1 && generalLog.Preparations == 1 && typedLog.Preparations == 1,
                "three compatible state views prepare the operator and each advice once in either resolution order");
            Check(generalLog.Bindings == 3 && typedLog.Bindings == 3 && total.Value == 3 && skippedLog.Preparations == 0,
                "state-dependent wrapper delegates are bound separately without affecting another operation's advice");
            var advised = narrow.Resolve(named, out ILiveValue? control);
            control!.Value = 12;
            Check(advised.IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem) && generalLog.Bindings == 3,
                "an advised terminator retains its live control and cached state-view constructor");
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
        Check(interceptor.Log.Constructions == 1 && interceptLog.Bindings == 1,
            "an incompatible raw view does not bind advice or construct another node");
    }

    [Fact]
    public void Ordering()
    {
        IOperator<RealVector, Mutation> source = new IdentityMutation();
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
        Check(observedCount == 1 && total.Value == 1 && localLog.Preparations == 1,
            "a duplicate advice neither counts twice nor moves its first position behind observation");

        var skipped = new AdviceLog { FailPreparation = true };
        var duplicate = new CountCalls(new CallCounter(), skipped);
        var firstMiss = ResolutionScope.Create(new Pairs((Pointcut.Predicate<IOperator>(_ => false), duplicate),
            (Pointcut.Reference(source), duplicate)));
        _ = Invoke(Typed(firstMiss).Resolve(source));
        Check(skipped.Preparations == 0, "first-position deduplication precedes matching, as in the shipping resolver");

        var sharedResult = new CallCounter();
        var first = new AdviceLog();
        var second = new AdviceLog();
        _ = Invoke(Typed(ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(source), new CountCalls(sharedResult, first)),
            Aspect.Create(Pointcut.Reference(source), new CountCalls(sharedResult, second)))).Resolve(source));
        Check(sharedResult.Value == 2 && first.Preparations == 1 && second.Preparations == 1,
            "distinct advice instances remain distinct even when their type and result accumulator match");
    }

    [Fact]
    public void ControlsAndContexts()
    {
        var source = new SharedOperations(new ProbeLog());
        IOperator<RealVector, Mutation> mutation = source;
        var inner = mutation.Named("inner");
        var outer = inner.Named("outer");
        var root = ResolutionScope.Create();
        var raw = Typed(root).Resolve(mutation, out ILiveValue? rawControl);
        var count = new CallCounter();
        var log = new AdviceLog();
        var scope = Typed(root.With(Aspect.Create(Pointcut.OfType<IOperator>(), new CountCalls(count, log))));
        var advised = scope.Resolve(outer, out ILiveValue? control);
        Check(ReferenceEquals(rawControl, control) && advised is not ILiveValue,
            "control projection forwards the child's provider through names and non-control inspection nodes");
        control!.Value = 17;
        Check(Invoke(advised) == 17 && count.Value == 3, "calls use the advised chain and count each of three selected configuration boundaries");
        Check(Invoke(raw) == 17 && count.Value == 3, "earlier raw bindings retain their original observation context");
        var rebound = scope.Resolve(outer, out ILiveValue? reboundControl);
        Check(ReferenceEquals(control, reboundControl) && Invoke(rebound) == 17 && count.Value == 6,
            "rebuilding the chain neither resets controls nor loses counting");
        Check(log.Preparations == 3 && log.Bindings == 3 && log.States.All(item => ((CallCountState)item.Target!).Calls == 2),
            "names and child have separately prepared advice state, reused on reconstruction");
        Check(new IOperator[] { source, inner, outer }.All(selected =>
            log.PreparedSources.Count(item => ReferenceEquals(item.Target, selected)) == 1), "each join point retains its selected source identity");

        IOperator<RealVector, Mutation> child = new CountingMutation();
        var composite = new ContextualMutation(child);
        var originalScope = ResolutionScope.Create();
        var original = Typed(originalScope).Resolve(composite);
        var childCounter = new CallCounter();
        var childLog = new AdviceLog();
        var contextual = Typed(originalScope.With(Aspect.Create(Pointcut.Reference(child), new CountCalls(childCounter, childLog)))).Resolve(composite);
        Check(Invoke(original) == 1 && Invoke(contextual) == 2 && Invoke(original) == 3 && childCounter.Value == 1,
            "newly resolved children acquire requesting advice while original parent nodes keep their old children");
        Check(Invoke(contextual) == 4 && childCounter.Value == 2 && childLog.Preparations == 1,
            "prepared child execution and advice state survive contextual reconstruction");
    }

    [Fact]
    public void Ownership()
    {
        foreach (var policy in new[] { ChildScopePolicy.Fresh, ChildScopePolicy.Retained, ChildScopePolicy.Shared })
        {
            IOperator<RealVector, Mutation> source = new CountingMutation();
            var log = new AdviceLog();
            var total = new CallCounter();
            var aspect = Aspect.Create(Pointcut.Reference(source), new CountCalls(total, log));
            var root = ResolutionScope.Create(aspect);
            var selector = new ChildScopeSelector(policy);
            var first = Typed(selector.GetScopeFor(source, root)).Resolve(source);
            var second = Typed(selector.GetScopeFor(source, root.With())).Resolve(source);
            Check(Invoke(first) == 1 && Invoke(second) == (policy == ChildScopePolicy.Fresh ? 1 : 2), $"{policy} preserves operator state semantics under advice");
            Check(total.Value == 2 && log.Preparations == (policy == ChildScopePolicy.Fresh ? 2 : 1), $"{policy} prepares advice in the selected operator owner");
            Check(policy == ChildScopePolicy.Fresh ? log.StateAt(0).Calls == 1 && log.StateAt(1).Calls == 1 : log.StateAt(0).Calls == 2,
                $"{policy} advice counters follow owner lifetime rather than contextual node lifetime");
        }
        IOperator<RealVector, Mutation> shared = new CountingMutation();
        var sharedLog = new AdviceLog();
        var sharedAspect = Aspect.Create(Pointcut.Reference(shared), new CountCalls(new CallCounter(), sharedLog));
        var parent = ResolutionScope.Create(sharedAspect);
        _ = Typed(parent).Resolve(shared);
        _ = Invoke(Typed(new ChildScopeSelector(ChildScopePolicy.Fresh).GetScopeFor(shared, parent)).Resolve(shared));
        Check(sharedLog.Preparations == 1 && sharedLog.StateAt(0).Calls == 1, "fresh activation reuses advice on an already ancestor-owned operator");
        var references = new AdviceLog();
        var referenceScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.OfType<IOperator>(), new CountCalls(new CallCounter(), references))));
        _ = Invoke(referenceScope.Resolve(new CountingMutation()));
        _ = Invoke(referenceScope.Resolve(new CountingMutation()));
        Check(references.Preparations == 2 && references.StateAt(0).Calls == 1 && references.StateAt(1).Calls == 1,
            "value-equal configuration records own separate prepared advice state");
    }

    [Fact]
    public void BindingFailures()
    {
        var child = new SharedOperations(new ProbeLog());
        var source = new ContextualMutation(child);
        var log = new AdviceLog { FailPreparation = true };
        var count = new CallCounter();
        var scope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CountCalls(count, log))));
        Reject(() => scope.Resolve(source), "advice preparation failure");
        Check(child.Log.Preparations == 0 && child.Log.Constructions == 0, "failed advice preparation cannot construct the raw child graph");
        log.FailPreparation = false;
        log.FailBinding = true;
        Reject(() => scope.Resolve(source), "advice binding failure");
        Check(log.Preparations == 2 && child.Log.Preparations == 0, "failed advice preparation retries; failed view binding leaves children untouched");
        log.FailBinding = false;
        Check(Invoke(scope.Resolve(source)) == 3 && log.Preparations == 2 && log.Bindings == 2 && count.Value == 1,
            "successful advice preparation survives a binding failure and its wrapper view can retry");

        var unbuiltChild = new SharedOperations(new ProbeLog());
        var wrongSource = new ContextualMutation(unbuiltChild);
        var wrongLog = new AdviceLog();
        var wrong = new WrongViewAdvice(new CallCounter(), wrongLog);
        var wrongScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(wrongSource), wrong)));
        var sentinel = new ControlSentinel();
        ILiveValue? control = sentinel;
        Reject(() => wrongScope.Resolve(wrongSource, out control), "cannot wrap");
        Check(unbuiltChild.Log.Preparations == 0 && ReferenceEquals(control, sentinel),
            "a malformed wrapper carrier fails before child construction or control exposure");
        wrong.ReturnWrongView = false;
        _ = Invoke(wrongScope.Resolve(wrongSource));
        Check(wrongLog.Preparations == 1 && wrongLog.Bindings == 2 && unbuiltChild.Log.Preparations == 1,
            "a failed carrier check is not cached as a successful wrapper and does not discard prepared advice state");

        var termSource = new SharedOperations(new ProbeLog());
        IOperator<RealVector, Termination> term = termSource;
        var generalLog = new AdviceLog();
        var typedLog = new AdviceLog();
        var generalTotal = new CallCounter();
        var typedTotal = new CallCounter();
        var root = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(term), new CountCalls(generalTotal, generalLog)),
            Aspect.Create(Pointcut.Reference(term), new CountCalls<RealVector, Termination>(typedTotal, typedLog)));
        var broad = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
        var narrow = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        _ = broad.Resolve(term).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
        typedLog.FailBinding = true;
        Reject(() => narrow.Resolve(term), "advice binding failure");
        Check(termSource.Log.Constructions == 1 && generalTotal.Value == 1 && typedTotal.Value == 1,
            "a failed second advice view invokes neither the raw constructor nor a success hook");
        _ = broad.Resolve(term).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
        Check(generalTotal.Value == 2 && typedTotal.Value == 2 && typedLog.Bindings == 2,
            "a failed second view leaves the first view's cached constructors and counters usable");
        typedLog.FailBinding = false;
        _ = narrow.Resolve(term).IsTerminalState(new ProbeState(), Problem.SearchSpace, Problem);
        Check(generalLog.Preparations == 1 && typedLog.Preparations == 1 && generalLog.Bindings == 2 && typedLog.Bindings == 3 &&
            generalTotal.Value == 3 && typedTotal.Value == 3, "retry reuses successful general binding and once-only advice state across views");

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
        Reject(() => scope.Resolve((IOperator<RealVector, Mutation>)source), "construction failure");
        source.Log.FailConstruction = false;
        _ = Invoke(scope.Resolve((IOperator<RealVector, Mutation>)source));
        Check(log.Preparations == 1 && log.Bindings == 1 && counter.Value == 1 && source.Log.Preparations == 1,
            "raw construction failure preserves prepared operator and advice factories");

        var construction = new ConstructionFailureAdvice();
        var constructionSource = new SharedOperations(new ProbeLog());
        var constructionScope = Typed(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(constructionSource), construction)));
        var sentinel = new ControlSentinel();
        ILiveValue? control = sentinel;
        RejectSame(() => constructionScope.Resolve((IOperator<RealVector, Mutation>)constructionSource, out control), construction.Failure);
        Check(ReferenceEquals(control, sentinel) && constructionSource.Log.Constructions == 1,
            "a wrapper constructor failure exposes no controls even though the raw node was constructed");
        construction.Fail = false;
        _ = constructionScope.Resolve((IOperator<RealVector, Mutation>)constructionSource, out control);
        Check(!ReferenceEquals(control, sentinel) && construction.Preparations == 1 && construction.Bindings == 1 &&
            constructionSource.Log.Preparations == 1, "wrapper construction retries over existing prepared owner and advice state");

        var invocation = new InvocationLog();
        IOperator<RealVector, Mutation> throwing = new ThrowingMutation(invocation);
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
        Check(inner.Value == 1 && outer.Value == 1, "failed and canceled child calls do not invoke success hooks");
        invocation.Failure = null;
        var callbackFailure = new InvalidOperationException("fixture callback failure");
        outerLog.AfterSuccess = () => throw callbackFailure;
        RejectSame(() => Invoke(operation), callbackFailure);
        Check(inner.Value == 2 && outer.Value == 2, "callback failure leaves already completed success updates intact");
        outerLog.AfterSuccess = null;
        _ = Invoke(operation);
        Check(inner.Value == 3 && outer.Value == 3 && invocation.Calls == 5, "later calls remain usable without extra child invocations");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Advice, WeakReference State) DiscardAdvice(ResolutionScope root, IOperator<RealVector, Mutation> source)
    {
        var log = new AdviceLog();
        var advice = new CountCalls(new CallCounter(), log);
        _ = Typed(root.With(Aspect.Create(Pointcut.Reference(source), advice))).Resolve(source);
        return (new WeakReference(advice), log.States[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Source, WeakReference Execution, WeakReference State) DiscardFreshState(ResolutionScope root, Aspect aspect, AdviceLog log)
    {
        IOperator<RealVector, Mutation> source = new CountingMutation();
        var executor = Typed(root.CreateChildScope(aspect)).Resolve(source);
        return (new WeakReference(source), new WeakReference(executor), log.States[^1]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Executor,
        WeakReference State, CallCounter Total) KeepExecution(ResolutionScope root, IOperator<RealVector, Mutation> source)
    {
        var log = new AdviceLog();
        var counter = new CallCounter();
        var executor = Typed(root.With(Aspect.Create(Pointcut.Reference(source), new CountCalls(counter, log)))).Resolve(source);
        return (executor, log.States[0], counter);
    }

    [Fact]
    public void Lifetime()
    {
        var root = ResolutionScope.Create();
        IOperator<RealVector, Mutation> source = new CountingMutation();
        var discardedAdvice = DiscardAdvice(root, source);
        var log = new AdviceLog();
        var advice = new CountCalls(new CallCounter(), log);
        var aspect = Aspect.Create(Pointcut.OfType<IOperator>(), advice);
        var discardedFresh = DiscardFreshState(root, aspect, log);
        var live = KeepExecution(root, source);
        for (var attempt = 0; attempt < 5 && (discardedAdvice.Advice.IsAlive || discardedAdvice.State.IsAlive ||
            discardedFresh.Source.IsAlive || discardedFresh.Execution.IsAlive || discardedFresh.State.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Check(!discardedAdvice.Advice.IsAlive && !discardedAdvice.State.IsAlive,
            "live owner does not retain a discarded advice key or its prepared state, including a key/value cycle");
        Check(!discardedFresh.Source.IsAlive && !discardedFresh.Execution.IsAlive && !discardedFresh.State.IsAlive,
            "a long-lived advice/aspect does not retain discarded fresh owners, sources or counter state");
        Check(live.State.IsAlive && Invoke(live.Executor) == 1 && live.Total.Value == 1,
            "a live advised execution retains the hook state it still needs");
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

    private sealed class UnsupportedAdvice : Advice<IOperator<RealVector, Mutation>>;

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

    private sealed class ConstructionFailureAdvice : OperatorAdvice<RealVector, Mutation>
    {
        public bool Fail { get; set; } = true;
        public int Preparations { get; private set; }
        public int Bindings { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("fixture wrapper constructor failure");

        public override OperatorAdviceWrapperFactory<RealVector, Mutation> CreateWrapperFactory(IOperator<RealVector, Mutation> source)
        {
            Preparations++;
            return new WrapperFactory(this);
        }

        private sealed class WrapperFactory(ConstructionFailureAdvice advice) : OperatorAdviceWrapperFactory<RealVector, Mutation>
        {
            public override WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TRunSearchState>(IOperator<RealVector, Mutation> source,
                OperationExecutorAdapter<RealVector, Mutation, TSearchSpace, TProblem, TRunSearchState> adapter)
            {
                advice.Bindings++;
                return WrapperExecutorFactory.From<IMutationExecutor<RealVector, TSearchSpace, TProblem>>(child =>
                    advice.Fail ? throw advice.Failure : child);
            }
        }
    }
}
