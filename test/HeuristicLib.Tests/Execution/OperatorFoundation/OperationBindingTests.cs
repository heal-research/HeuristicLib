using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class OperationBindingTests
{
    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));
    private static readonly RealVector[] Candidates = [new(1.0), new(2.0), new(3.0)];

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 0)]
    [InlineData(8, 0)]
    public void EveryOperationPreservesItsDomainCallAndReportsDeclaredWork(int operation, int expectedWork)
    {
        var source = new AllOperations();
        var probe = new HookProbe();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new ProbeAdvice(probe)));
        InvokeOperation(scope, source, operation);
        probe.Work.ShouldBe(expectedWork);
        probe.Successes.ShouldBe(1);
        probe.Cleanups.ShouldBe(1);
        source.Invocations.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryOperationRetriesItsNamedCallWithoutReconstructingTheChild(int operation)
    {
        foreach (var failures in new[] { 0, 2, 3 })
        {
            var failure = new RetryableOperationException("again");
            var source = new AllOperations { Failure = failure, FailuresBeforeSuccess = failures };
            var error = Record.Exception(() => InvokeOperation(ResolutionScope.Create(), source, operation, retry: true));
            if (failures < 3)
                error.ShouldBeNull();
            else
                error.ShouldBeSameAs(failure);
            source.Invocations.ShouldBe(Math.Min(failures + 1, 3));
            source.Preparations.ShouldBe(1);
            source.Constructions.ShouldBe(1);
            source.RandomSources[0].ShouldNotBeNull();
            source.RandomSources.ShouldAllBe(random => ReferenceEquals(random, source.RandomSources[0]));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryOperationSupportsAnInvocationWrapperThatInvokesItsChildTwice(int operation)
    {
        var source = new AllOperations();
        var advice = new TwiceAdvice();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), advice));

        InvokeOperation(scope, source, operation);

        advice.Wraps.ShouldBe(1);
        source.Preparations.ShouldBe(1);
        source.Constructions.ShouldBe(1);
        source.Invocations.ShouldBe(2);
        source.RandomSources.ShouldAllBe(random => ReferenceEquals(random, source.RandomSources[0]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryOperationExposesTheCallsRandomAndProceedsWithAnotherRandom(int operation)
    {
        var source = new AllOperations();
        var fork = RandomNumberGenerator.Create(2);
        var recorder = new InvocationRecorder();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new RedirectingAdvice(null, fork, recorder)));

        InvokeOperation(scope, source, operation);

        source.RandomSources.ShouldBe([fork]);
        recorder.Random.ShouldNotBeNull().ShouldNotBeSameAs(fork);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryOperationProceedsWithAnotherExecutorOfTheSameOperation(int operation)
    {
        var source = new AllOperations();
        var other = new AllOperations();
        var recorder = new InvocationRecorder();
        var otherExecutor = ResolveOperation(ResolutionScope.Create(), other, operation);
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new RedirectingAdvice(otherExecutor, null, recorder)));

        InvokeOperation(scope, source, operation);

        source.Invocations.ShouldBe(0);
        other.Invocations.ShouldBe(1);
        other.RandomSources.ShouldBe([recorder.Random.ShouldNotBeNull()]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryOperationForwardsControlsUnlessItsWrapperSuppliesThem(int operation)
    {
        var source = new AllOperations();
        var root = ResolutionScope.Create();
        var raw = ResolveOperation(root, source, operation);
        var forwarding = ResolveOperation(root.With(Aspect.Create(Pointcut.Reference(source), new TwiceAdvice())), source, operation);
        var supplied = new SuppliedControl();
        var supplying = ResolveOperation(root.With(Aspect.Create(Pointcut.Reference(source), new ControlSupplyingAdvice(supplied))), source, operation);

        // Each build creates its own executor, so the advised build forwards the control of its own raw executor.
        forwarding.ShouldNotBeOfType(raw.GetType());
        ExecutorControls.Find<IExecutor>(forwarding).ShouldBeOfType(raw.GetType());
        ExecutorControls.Find<SuppliedControl>(supplying).ShouldBeSameAs(supplied);
        ExecutorControls.Find<IExecutor>(supplying).ShouldBeNull();
    }

    [Fact]
    public void InvocationWrappersBindOncePerExecutorAndRetainTheirOwnChildAdvice()
    {
        var source = new AllOperations();
        IOperator<Mutation, RealVector> mutation = source;
        var factory = new TwiceAdvice();
        var root = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), factory));
        var first = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(mutation);
        factory.Wraps.ShouldBe(1);
        source.Invocations.ShouldBe(0);
        var random = RandomNumberGenerator.Create(1);
        for (var i = 0; i < 3; i++)
            first.Mutate(Candidates, random, Problem.SearchSpace, Problem).ShouldBeSameAs(AllOperations.Output);
        factory.Wraps.ShouldBe(1);

        var contextCalls = new CountAccumulator();
        var second = root.With(Aspect.CountInvocations(mutation, contextCalls))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(mutation);
        factory.Wraps.ShouldBe(2);
        second.Mutate(Candidates, random, Problem.SearchSpace, Problem).ShouldBeSameAs(AllOperations.Output);
        contextCalls.CurrentCount.ShouldBe(2);
        first.Mutate(Candidates, random, Problem.SearchSpace, Problem).ShouldBeSameAs(AllOperations.Output);
        contextCalls.CurrentCount.ShouldBe(2);
        source.Preparations.ShouldBe(1);
        source.Constructions.ShouldBe(2);
        source.Invocations.ShouldBe(10);
        factory.Wraps.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryOperationReportsFaultAndCleansUpOnce(int operation)
    {
        var failure = new OperationCanceledException("operation canceled");
        var source = new AllOperations { Failure = failure };
        var probe = new HookProbe();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new ProbeAdvice(probe)));
        Assert.Throws<OperationCanceledException>(() => InvokeOperation(scope, source, operation)).ShouldBeSameAs(failure);
        probe.Failure.ShouldBeSameAs(failure);
        probe.Successes.ShouldBe(0);
        probe.Cleanups.ShouldBe(1);
        source.Invocations.ShouldBe(1);
    }

    [Theory]
    [InlineData(7, "Termination")]
    [InlineData(8, "Interception")]
    public void CandidateMetricRejectionPrecedesConstructionAndScopedState(int operation, string name)
    {
        var source = new AllOperations();
        var root = ResolutionScope.Create();
        var measured = root.With(Aspect.Create(Pointcut.Reference(source), new CandidateMetric()));
        var failure = Assert.Throws<InvalidOperationException>(() => InvokeOperation(measured, source, operation));
        failure.Message.ShouldContain(name);
        failure.Message.ShouldContain("candidate budget");
        failure.Message.ShouldContain(nameof(AllOperations));
        source.Constructions.ShouldBe(0);
        // The rejection precedes the construction hook, so it also precedes the scoped state the hook creates.
        source.Preparations.ShouldBe(0);
        InvokeOperation(root, source, operation);
        source.Constructions.ShouldBe(1);
        source.Preparations.ShouldBe(1);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    public void OperationsWithoutCandidateWorkReportNoneAndThrowWhenAskedForIt(int operation)
    {
        var source = new AllOperations();
        var probe = new HookProbe();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CandidateWorkAdvice(probe)));

        Assert.Throws<NotSupportedException>(() => InvokeOperation(scope, source, operation));
        probe.Successes.ShouldBe(0);
        source.Invocations.ShouldBe(1);
    }

    [Fact]
    public void UnsupportedExternalScalarMetricRejectsBeforeItsChildIsResolved()
    {
        var child = new SharedOperations(new ProbeLog());
        var source = new Forward<Checksum, RealVector>(child);
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CandidateMetric()));
        var failure = Assert.Throws<InvalidOperationException>(() =>
            scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source));
        failure.Message.ShouldContain(nameof(Checksum));
        child.Log.Preparations.ShouldBe(0);
    }

    private static IExecutor ResolveOperation(ResolutionScope scope, AllOperations source, int operation)
    {
        var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        return operation switch
        {
            0 => typed.ResolveOperator((IOperator<Creation, RealVector>)source),
            1 => typed.ResolveOperator((IOperator<Crossover, RealVector>)source),
            2 => typed.ResolveOperator((IOperator<Mutation, RealVector>)source),
            3 => typed.ResolveOperator((IOperator<Evaluation, RealVector>)source),
            4 => typed.ResolveOperator((IOperator<Selection, RealVector>)source),
            5 => typed.ResolveOperator((IOperator<Refinement, RealVector>)source),
            6 => typed.ResolveOperator((IOperator<Replacement, RealVector>)source),
            7 => typed.ResolveOperator((IOperator<Termination, RealVector>)source),
            8 => typed.ResolveOperator((IOperator<Interception<ProbeState>, RealVector>)source),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static void InvokeOperation(ResolutionScope scope, AllOperations source, int operation, bool retry = false)
    {
        var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        var random = RandomNumberGenerator.Create(1);
        var space = Problem.SearchSpace;
        var population = Candidates.ToEvaluated<RealVector>([1.0, 2.0, 3.0]);
        IOperator<TOperation, RealVector> Wrap<TOperation>(IOperator<TOperation, RealVector> child)
            where TOperation : IOperation<TOperation> => retry ? child.Retry(3) : child;
        switch (operation)
        {
            case 0:
                typed.Resolve(Wrap((IOperator<Creation, RealVector>)source)).Create(3, random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 1:
                typed.Resolve(Wrap((IOperator<Crossover, RealVector>)source)).Cross([new(Candidates[0], Candidates[1])], random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 2:
                typed.Resolve(Wrap((IOperator<Mutation, RealVector>)source)).Mutate(Candidates, random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 3:
                typed.Resolve(Wrap((IOperator<Evaluation, RealVector>)source)).Evaluate(Candidates, random, space, Problem).ShouldBeEmpty();
                break;
            case 4:
                typed.Resolve(Wrap((IOperator<Selection, RealVector>)source)).Select(population, Problem.Objective, 3, random, space, Problem).ShouldBeSameAs(AllOperations.Population);
                break;
            case 5:
                typed.Resolve(Wrap((IOperator<Refinement, RealVector>)source)).Refine(Candidates, random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 6:
                typed.Resolve(Wrap((IOperator<Replacement, RealVector>)source)).Replace(population, population, Problem.Objective, 3, random, space, Problem).ShouldBeSameAs(AllOperations.Population);
                break;
            case 7:
                typed.Resolve(Wrap((IOperator<Termination, RealVector>)source)).IsTerminalState(new ProbeState(), random, space, Problem).ShouldBeTrue();
                break;
            case 8:
                var state = new ProbeState();
                typed.Resolve(Wrap((IOperator<Interception<ProbeState>, RealVector>)source)).Transform(state, null, random, space, Problem).ShouldBeSameAs(state);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private sealed class HookProbe
    {
        public int Work { get; set; }
        public int Successes { get; set; }
        public int Cleanups { get; set; }
        public Exception? Failure { get; set; }
    }

    private readonly struct ProbeWrapper(HookProbe probe, bool countsWork) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            try
            {
                var result = invocation.Proceed();
                probe.Work = countsWork ? invocation.CountCandidateWork(result) : 0;
                probe.Successes++;
                return result;
            }
            catch (Exception failure)
            {
                probe.Failure = failure;
                throw;
            }
            finally
            {
                probe.Cleanups++;
            }
        }
    }

    private sealed class ProbeAdvice(HookProbe probe) : OperatorAdvice
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source) =>
            operation.Wrap(executor, new ProbeWrapper(probe, operation.SupportsCandidateWork));
    }

    // Asks for candidate work without checking the operation, which only a faulty wrapper does.
    private sealed class CandidateWorkAdvice(HookProbe probe) : OperatorAdvice
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source)
        {
            operation.SupportsCandidateWork.ShouldBeFalse();
            return operation.Wrap(executor, new ProbeWrapper(probe, countsWork: true));
        }
    }

    private readonly struct Twice : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            _ = invocation.Proceed();
            return invocation.Proceed();
        }
    }

    private sealed class TwiceAdvice : OperatorAdvice
    {
        public int Wraps { get; private set; }

        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source)
        {
            Wraps++;
            return operation.Wrap(executor, new Twice());
        }
    }

    private sealed class InvocationRecorder
    {
        public IRandomNumberGenerator? Random { get; set; }
    }

    private readonly struct RedirectingWrapper(IExecutor? executor, IRandomNumberGenerator? random, InvocationRecorder recorder) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            recorder.Random = invocation.Random;
            return invocation.Proceed(executor, random);
        }
    }

    private sealed class RedirectingAdvice(IExecutor? executor, IRandomNumberGenerator? random, InvocationRecorder recorder) : OperatorAdvice
    {
        public override IExecutor Wrap(IExecutor wrapped, OperationInfo operation, IOperator source) =>
            operation.Wrap(wrapped, new RedirectingWrapper(executor, random, recorder));
    }

    private sealed class SuppliedControl;

    private readonly struct ControlSupplyingWrapper(SuppliedControl control) : IInvocationWrapper, IExecutorControlProvider
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult> =>
            invocation.Proceed();

        public TControl? Get<TControl>() where TControl : class => control as TControl;
    }

    private sealed class ControlSupplyingAdvice(SuppliedControl control) : OperatorAdvice
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source) =>
            operation.Wrap(executor, new ControlSupplyingWrapper(control));
    }

    private sealed class CandidateMetric : OperatorAdvice
    {
        public override void Check(OperationInfo operation, IOperator source) =>
            operation.RequireCandidateWork(source, "candidate budget");

        public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source) => executor;
    }

    private sealed class AllOperations : IOperator<Creation, RealVector>, IOperator<Crossover, RealVector>,
        IOperator<Mutation, RealVector>, IOperator<Evaluation, RealVector>, IOperator<Selection, RealVector>,
        IOperator<Refinement, RealVector>, IOperator<Replacement, RealVector>, IOperator<Termination, RealVector>,
        IOperator<Interception<ProbeState>, RealVector>
    {
        public static readonly RealVector[] Output = [new(4.0), new(5.0)];
        public static readonly IReadOnlyList<EvaluatedCandidate<RealVector>> Population = Output.ToEvaluated<RealVector>([4.0, 5.0]);
        public int Preparations { get; private set; }
        public int Constructions { get; private set; }
        public int Invocations { get; private set; }
        public Exception? Failure { get; init; }
        public int FailuresBeforeSuccess { get; init; } = int.MaxValue;
        public List<IRandomNumberGenerator> RandomSources { get; } = [];

        // One public method implements every operation's CreateExecutor, whose signature does not name the operation.
        // Creating the scoped state counts as the preparation; every build creates its own executor.
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            scope.GetScopedState(this, () =>
            {
                Preparations++;
                return new Preparation();
            });
            Constructions++;
            return new Executor(this);
        }

        private sealed class Preparation;

        private void Call(IRandomNumberGenerator random)
        {
            RandomSources.Add(random);
            Invocations++;
            if (Failure is { } failure && Invocations <= FailuresBeforeSuccess)
                throw failure;
        }

        private sealed class Executor(AllOperations source) :
            ICreationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            ICrossoverExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            IMutationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            IEvaluationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            ISelectionExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            IRefinementExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            IReplacementExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
            ITerminationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, ISearchState>,
            IInterceptionExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, ProbeState>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return Output; }
            public IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return Output; }
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return Output; }
            // Empty objective output deliberately distinguishes input work from result cardinality.
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return []; }
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return Population; }
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return Output; }
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return Population; }
            public bool IsTerminalState(ISearchState state, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return true; }
            public ProbeState Transform(ProbeState currentState, ProbeState? previousState, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(random); return currentState; }
        }
    }
}
