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
            var generalTotal = new InvocationCounter();
            var generalLog = new AdviceLog();
            var typedTotal = new InvocationCounter();
            var typedLog = new AdviceLog();
            var general = Aspect.Create(Pointcut.Reference(source), new CountInvocations(generalTotal, generalLog));
            var typedAdvice = Aspect.Create(Pointcut.Reference(mutation), new CountInvocations<Mutation, RealVector>(typedTotal, typedLog));
            var root = ResolutionScope.Create();
            root.AddAspect(general);
            root.AddAspect(typedAdvice);
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
            _ = Invoke(Typed(root.CreateChildScope()).Resolve(mutation));
            Check(generalLog.Wraps == 5 && typedLog.Wraps == 2 && generalTotal.Value == 5 && typedTotal.Value == 2,
                "contextual reconstruction wraps the new executor with the same advice objects");
            var advisedScope = ResolutionScope.Create();
            advisedScope.AddAspect(general);
            advisedScope.AddAspect(typedAdvice);
            _ = Invoke(Typed(advisedScope).Resolve(mutation));
            Check(source.Log.Preparations == 3 && generalTotal.Value == 6,
                "each scope prepares operator state once for all operations while advice data stays on the advice");
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
            var total = new InvocationCounter();
            var root = ResolutionScope.Create();
            root.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountInvocations(total, generalLog)));
            root.AddAspect(Aspect.Create(Pointcut.Reference(term), new CountInvocations<Termination, RealVector>(new InvocationCounter(), typedLog)));
            root.AddAspect(Aspect.Create(Pointcut.Reference((IOperator<Mutation, RealVector>)source),
                    new CountInvocations<Mutation, RealVector>(new InvocationCounter(), skippedLog)));
            var broad = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>();
            var narrow = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
            _ = narrowFirst
                ? narrow.Resolve(named).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem)
                : broad.Resolve(named).IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            // Each child scope builds its own executor for the state view it requests.
            _ = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>().Resolve(named)
                .IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            _ = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, OtherState>().Resolve(named)
                .IsTerminalState(new OtherState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem);
            Check(source.Log.Preparations == 3, "each scope constructs an executor for its state view");
            Check(generalLog.Wraps == 3 && typedLog.Wraps == 3 && total.Value == 3 && skippedLog.Checks == 0,
                "state views are wrapped separately without applying another operation's advice");
            // The names' executors hold the terminator as advised for the first view. A narrower view can use them; a
            // broader one cannot.
            var advised = narrow.Resolve(named, out ILiveValue? control);
            Check(source.Log.Preparations == 3 && generalLog.Wraps == 3, "a compatible view in the same scope uses the executors built first");
            if (narrowFirst)
                Reject(() => broad.Resolve(named), "already built in this resolution scope");
            control!.Value = 12;
            Check(advised.IsTerminalState(new ProbeState(), RandomNumberGenerator.Create(0), Problem.SearchSpace, Problem), "an advised terminator retains its live control");
        }
        var interceptor = new IdentityInterceptor(new ProbeLog());
        var interceptTotal = new InvocationCounter();
        var interceptLog = new AdviceLog();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(interceptor), new CountInvocations(interceptTotal, interceptLog)));
        var exact = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        var state = new ProbeState();
        Check(ReferenceEquals(state, exact.Resolve(interceptor).Transform(state, null, RandomNumberGenerator.Create(1),
            Problem.SearchSpace, Problem)) && interceptTotal.Value == 1, "the same general counter works on interception without altering its result");
        _ = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>().Resolve(interceptor);
        Check(interceptor.Log.Constructions == 1 && interceptLog.Wraps == 2,
            "a view over another state wraps the scope's interceptor over the state its operation names");
    }

    [Fact]
    public void Ordering()
    {
        IOperator<Mutation, RealVector> source = new IdentityMutation();
        var order = new List<string>();
        var total = new InvocationCounter();
        var localLog = new AdviceLog { AfterSuccess = () => order.Add("local") };
        var local = new CountInvocations(total, localLog);
        var observedCount = 0;
        var observer = new CountInvocations(new InvocationCounter(), new AdviceLog
        {
            AfterSuccess = () =>
        {
            order.Add("observer");
            observedCount = total.Value;
        }
        });
        var parentA = new CountInvocations(new InvocationCounter(), new AdviceLog { AfterSuccess = () => order.Add("parent-a") });
        var parentB = new CountInvocations(new InvocationCounter(), new AdviceLog { AfterSuccess = () => order.Add("parent-b") });
        var parent = ResolutionScope.Create();
        parent.AddAspect(Aspect.Create(Pointcut.Reference(source), parentA));
        parent.AddAspect(Aspect.Create(Pointcut.Reference(source), parentB));
        var scope = parent.CreateChildScope();
        scope.AddAspect(new Pairs((Pointcut.Reference(source), local), (Pointcut.Reference(source), observer),
            (Pointcut.Reference(source), local)));
        _ = Invoke(Typed(scope).Resolve(source));
        Check(order.SequenceEqual(new[] { "local", "observer", "parent-b", "parent-a" }),
            "successful callbacks run in application order: the closest scope first, the aspect added last first, and pairs in listed order");
        Check(observedCount == 1 && total.Value == 1 && localLog.Wraps == 1,
            "a duplicate advice neither counts twice nor moves its first position behind observation");

        var skipped = new AdviceLog { FailCheck = true };
        var duplicate = new CountInvocations(new InvocationCounter(), skipped);
        var firstMiss = ResolutionScope.Create();
        firstMiss.AddAspect(new Pairs((Pointcut.Predicate<IOperator>(_ => false), duplicate),
            (Pointcut.Reference(source), duplicate)));
        Reject(() => Typed(firstMiss).Resolve(source), "advice check failure");
        Check(skipped.Checks == 1, "matching precedes deduplication, so the advice applies through the pairing that matches");

        var sharedResult = new InvocationCounter();
        var first = new AdviceLog();
        var second = new AdviceLog();
        var advisedScope = ResolutionScope.Create();
        advisedScope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountInvocations(sharedResult, first)));
        advisedScope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountInvocations(sharedResult, second)));
        _ = Invoke(Typed(advisedScope).Resolve(source));
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
        var count = new InvocationCounter();
        var log = new AdviceLog();
        var scopeScope = root.CreateChildScope();
        scopeScope.AddAspect(Aspect.Create(Pointcut.OfType<IOperator>(), new CountInvocations(count, log)));
        var scope = Typed(scopeScope);
        var advised = scope.Resolve(outer, out ILiveValue? control);
        // Each resolution builds its own executor and its control targets that executor.
        Check(rawControl is not null && control is not null && advised is not ILiveValue,
            "control lookup follows names and non-control inspection wrappers to the source");
        control!.Value = 17;
        Check(rawControl!.Value == 3, "separately resolved controls belong to separate executors");
        Check(Invoke(advised) == 17 && count.Value == 3, "calls use the advised chain and count each of three selected configuration boundaries");
        Check(Invoke(raw) == 3 && count.Value == 3, "earlier raw bindings retain their original observation context");
        var rebound = scope.Resolve(outer, out ILiveValue? reboundControl);
        Check(reboundControl is not null && reboundControl.Value == 17 && Invoke(rebound) == 17 && count.Value == 6,
            "a second request reuses the scope's executors and retains the supplied counter");
        Check(log.Wraps == 4 && log.WrappedSources.Count(item => ReferenceEquals(item.Target, outer)) == 2 &&
            new IOperator[] { source, inner }.All(selected => log.WrappedSources.Count(item => ReferenceEquals(item.Target, selected)) == 1),
            "each request wraps its configuration with its own source identity around the executors the scope built");

        IOperator<Mutation, RealVector> child = new CountingMutation();
        var composite = new ContextualMutation(child);
        var originalScope = ResolutionScope.Create();
        var original = Typed(originalScope).Resolve(composite);
        var childCounter = new InvocationCounter();
        var childLog = new AdviceLog();
        var contextualScope = originalScope.CreateChildScope();
        contextualScope.AddAspect(Aspect.Create(Pointcut.Reference(child), new CountInvocations(childCounter, childLog)));
        var contextual = Typed(contextualScope).Resolve(composite);
        Check(Invoke(original) == 1 && Invoke(contextual) == 1 && Invoke(original) == 2 && childCounter.Value == 1,
            "newly resolved children acquire requesting advice while original parent nodes keep their old children");
        Check(Invoke(contextual) == 2 && childCounter.Value == 2 && childLog.Wraps == 1,
            "the contextual executor retains its own child fields");
    }

    [Fact]
    public void Ownership()
    {
        foreach (var policy in new[] { false, true })
        {
            IOperator<Mutation, RealVector> source = new CountingMutation();
            var log = new AdviceLog();
            var total = new InvocationCounter();
            var aspect = Aspect.Create(Pointcut.Reference(source), new CountInvocations(total, log));
            var root = ResolutionScope.Create();
            root.AddAspect(aspect);
            var first = Typed(root).Resolve(source);
            var second = Typed(root.CreateChildScope()).Resolve(source);
            Check(Invoke(first) == 1 && Invoke(second) == 1, $"{policy} preserves operator state semantics under advice");
            Check(total.Value == 2 && log.Wraps == 2, $"{policy} applies the advice to every activation");
        }
        var references = new AdviceLog();
        var referenceRoot = ResolutionScope.Create();
        referenceRoot.AddAspect(Aspect.Create(Pointcut.OfType<IOperator>(), new CountInvocations(new InvocationCounter(), references)));
        var referenceScope = Typed(referenceRoot);
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
        var count = new InvocationCounter();
        var aspect = Aspect.Create(Pointcut.Reference(source), new CountInvocations(count, log));
        var advisedRoot = ResolutionScope.Create();
        advisedRoot.AddAspect(aspect);
        var scope = Typed(advisedRoot);
        Reject(() => scope.Resolve(source), "advice check failure");
        Check(child.Log.Preparations == 0 && child.Log.Constructions == 0, "a failed advice check precedes the raw child graph");
        log.FailCheck = false;
        log.FailWrap = true;
        Reject(() => scope.Resolve(source), "advice wrapping failure");
        Check(child.Log.Preparations == 1 && child.Log.Constructions == 1, "wrapping follows raw construction");
        Reject(() => scope.Resolve(source), "Resolve through a new scope");
        log.FailWrap = false;
        var advisedScope = ResolutionScope.Create();
        advisedScope.AddAspect(aspect);
        Check(Invoke(Typed(advisedScope).Resolve(source)) == 3 && log.Checks == 3 && log.Wraps == 2 && count.Value == 1 &&
            child.Log.Preparations == 2, "a failed wrap ends the scope tree, and a new scope constructs a fresh child");

        var wrongChild = new SharedOperations(new ProbeLog());
        var wrongSource = new ContextualMutation(wrongChild);
        var wrongLog = new AdviceLog();
        var wrong = new WrongViewAdvice(new InvocationCounter(), wrongLog);
        var wrongAspect = Aspect.Create(Pointcut.Reference(wrongSource), wrong);
        var wrongRoot = ResolutionScope.Create();
        wrongRoot.AddAspect(wrongAspect);
        var wrongScope = Typed(wrongRoot);
        var sentinel = new ControlSentinel();
        ILiveValue? control = sentinel;
        Reject(() => wrongScope.Resolve(wrongSource, out control), "cannot provide");
        Check(ReferenceEquals(control, sentinel), "advice returning another executor type exposes no controls");
        wrong.ReturnWrongView = false;
        var retry = ResolutionScope.Create();
        retry.AddAspect(wrongAspect);
        _ = Invoke(Typed(retry).Resolve(wrongSource));
        Check(wrongLog.Wraps == 2 && wrongChild.Log.Preparations == 2, "a rejected advice result ends the scope tree, and a new scope constructs a fresh child");

        var termSource = new SharedOperations(new ProbeLog());
        IOperator<Termination, RealVector> term = termSource;
        var generalLog = new AdviceLog();
        var typedLog = new AdviceLog();
        var generalTotal = new InvocationCounter();
        var typedTotal = new InvocationCounter();
        var root = ResolutionScope.Create();
        root.AddAspect(Aspect.Create(Pointcut.Reference(term), new CountInvocations(generalTotal, generalLog)));
        root.AddAspect(Aspect.Create(Pointcut.Reference(term), new CountInvocations<Termination, RealVector>(typedTotal, typedLog)));
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
            "a retried view uses the scope's executor in wrappers of its own");

        var unsupported = new UnsupportedAdvice();
        var unsupportedRoot = ResolutionScope.Create();
        unsupportedRoot.AddAspect(Aspect.Create(Pointcut.Reference(source), unsupported));
        var unsupportedScope = Typed(unsupportedRoot);
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
        var counter = new InvocationCounter();
        var aspect = Aspect.Create(Pointcut.Reference(source), new CountInvocations(counter, log));
        var root = ResolutionScope.Create();
        root.AddAspect(aspect);
        var scope = Typed(root);
        Reject(() => scope.Resolve((IOperator<Mutation, RealVector>)source), "construction failure");
        Check(log.Checks == 1 && log.Wraps == 0, "advice checks precede raw construction; wrapping follows it");
        source.Log.FailConstruction = false;
        var advisedScope = ResolutionScope.Create();
        advisedScope.AddAspect(aspect);
        _ = Invoke(Typed(advisedScope).Resolve((IOperator<Mutation, RealVector>)source));
        Check(log.Wraps == 1 && counter.Value == 1 && source.Log.Preparations == 2,
            "a new scope constructs fresh private data after a raw construction failure");

        var construction = new ConstructionFailureAdvice();
        var constructionSource = new SharedOperations(new ProbeLog());
        var constructionAspect = Aspect.Create(Pointcut.Reference(constructionSource), construction);
        var constructionRoot = ResolutionScope.Create();
        constructionRoot.AddAspect(constructionAspect);
        var constructionScope = Typed(constructionRoot);
        var sentinel = new ControlSentinel();
        ILiveValue? control = sentinel;
        RejectSame(() => constructionScope.Resolve((IOperator<Mutation, RealVector>)constructionSource, out control), construction.Failure);
        Check(ReferenceEquals(control, sentinel) && constructionSource.Log.Constructions == 1,
            "a wrapper constructor failure exposes no controls even though the raw node was constructed");
        construction.Fail = false;
        var retry = ResolutionScope.Create();
        retry.AddAspect(constructionAspect);
        _ = Typed(retry).Resolve((IOperator<Mutation, RealVector>)constructionSource, out control);
        Check(!ReferenceEquals(control, sentinel) && construction.Wraps == 2 && constructionSource.Log.Preparations == 2,
            "a new scope retries wrapper construction with fresh private data");

        var invocation = new InvocationLog();
        IOperator<Mutation, RealVector> throwing = new ThrowingMutation(invocation);
        var inner = new InvocationCounter();
        var outer = new InvocationCounter();
        var outerLog = new AdviceLog();
        var operationScope = ResolutionScope.Create();
        operationScope.AddAspect(Aspect.Create(Pointcut.Reference(throwing), new CountInvocations(outer, outerLog)));
        operationScope.AddAspect(Aspect.Create(Pointcut.Reference(throwing), new CountInvocations(inner, new AdviceLog())));
        var operation = Typed(operationScope).Resolve(throwing);
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
        Check(inner.Value == 3 && outer.Value == 3 && invocation.Invocations == 5, "later calls remain usable without extra child invocations");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DiscardAdvice(ResolutionScope root, IOperator<Mutation, RealVector> source)
    {
        var advice = new CountInvocations(new InvocationCounter(), new AdviceLog());
        var scope = root.CreateChildScope();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), advice));
        _ = Typed(scope).Resolve(source);
        return new WeakReference(advice);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Source, WeakReference Execution) DiscardFreshState(ResolutionScope root, Aspect aspect)
    {
        IOperator<Mutation, RealVector> source = new CountingMutation();
        var scope = root.CreateChildScope();
        scope.AddAspect(aspect);
        var executor = Typed(scope).Resolve(source);
        return (new WeakReference(source), new WeakReference(executor));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Executor,
        WeakReference Advice, InvocationCounter Total) KeepExecution(ResolutionScope root, IOperator<Mutation, RealVector> source)
    {
        var counter = new InvocationCounter();
        var advice = new CountInvocations(counter, new AdviceLog());
        var scope = root.CreateChildScope();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), advice));
        var executor = Typed(scope).Resolve(source);
        return (executor, new WeakReference(advice), counter);
    }

    [Fact]
    public void Lifetime()
    {
        var root = ResolutionScope.Create();
        IOperator<Mutation, RealVector> source = new CountingMutation();
        var discardedAdvice = DiscardAdvice(root, source);
        var advice = new CountInvocations(new InvocationCounter(), new AdviceLog());
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
        Check(!discardedAdvice.IsAlive, "a live context does not retain discarded advice");
        Check(!discardedFresh.Source.IsAlive && !discardedFresh.Execution.IsAlive,
            "a live aspect does not retain discarded executors or sources");
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
        public int Invocations { get; set; }
        public Exception? Failure { get; set; }
    }

    private sealed record ThrowingMutation(InvocationLog Log) : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
        {
            Log.Invocations++;
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
