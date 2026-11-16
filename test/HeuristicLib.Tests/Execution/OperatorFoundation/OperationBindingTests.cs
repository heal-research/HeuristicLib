using System.Diagnostics;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
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
        var scope = ResolutionScope.Create();
        InvokeOperation(scope, source, operation, new ProbeWrapping(probe));
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
        var twice = new TwiceWrapping();
        var scope = ResolutionScope.Create();

        InvokeOperation(scope, source, operation, twice);

        twice.Wraps.ShouldBe(1);
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
        var scope = ResolutionScope.Create();

        InvokeOperation(scope, source, operation, new RedirectingWrapping(null, fork, recorder));

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
        var scope = ResolutionScope.Create();

        InvokeOperation(scope, source, operation, new RedirectingWrapping(otherExecutor, null, recorder));

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
        var forwardingScope = root.CreateChildScope();
        var forwarding = ResolveOperation(forwardingScope, source, operation, new TwiceWrapping());
        var supplied = new SuppliedControl();
        var supplyingScope = root.CreateChildScope();
        var supplying = ResolveOperation(supplyingScope, source, operation, new ControlSupplyingWrapping(supplied));

        // Each build creates its own executor, so the wrapped build forwards the control of its own raw executor.
        forwarding.ShouldNotBeOfType(raw.GetType());
        ExecutorControls.Find<IExecutor>(forwarding).ShouldBeOfType(raw.GetType());
        ExecutorControls.Find<SuppliedControl>(supplying).ShouldBeSameAs(supplied);
        ExecutorControls.Find<IExecutor>(supplying).ShouldBeNull();
    }

    [Fact]
    public void InvocationWrappersBindOncePerExecutorAndRetainTheirOwnChildWrappers()
    {
        var source = new AllOperations();
        IOperator<Mutation, RealVector> mutation = source;
        var factory = new TwiceWrapping();
        IOperator<Mutation, RealVector> wrapped = new Wrapped<Mutation>(mutation, factory);
        var root = ResolutionScope.Create();
        var first = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem).Resolve(wrapped);
        factory.Wraps.ShouldBe(1);
        source.Invocations.ShouldBe(0);
        var random = RandomNumberGenerator.Create(1);
        for (var i = 0; i < 3; i++)
            first.Mutate(Candidates, random).ShouldBeSameAs(AllOperations.Output);
        factory.Wraps.ShouldBe(1);

        var counted = wrapped.Counting(mutation, out var calls);
        var contextCalls = root.Results.Result(calls);
        var secondScope = root.CreateChildScope();
        var second = secondScope
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem).Resolve(counted);
        factory.Wraps.ShouldBe(2);
        second.Mutate(Candidates, random).ShouldBeSameAs(AllOperations.Output);
        contextCalls.CurrentCount.ShouldBe(2);
        first.Mutate(Candidates, random).ShouldBeSameAs(AllOperations.Output);
        contextCalls.CurrentCount.ShouldBe(2);
        source.Preparations.ShouldBe(2);
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
        var scope = ResolutionScope.Create();
        Assert.Throws<OperationCanceledException>(() => InvokeOperation(scope, source, operation, new ProbeWrapping(probe))).ShouldBeSameAs(failure);
        probe.Failure.ShouldBeSameAs(failure);
        probe.Successes.ShouldBe(0);
        probe.Cleanups.ShouldBe(1);
        source.Invocations.ShouldBe(1);
    }

    [Theory]
    [InlineData(7, "Termination")]
    [InlineData(8, "Interception")]
    public void CandidateMetricRejectionPrecedesPrivateDataConstruction(int operation, string name)
    {
        var source = new AllOperations();
        var root = ResolutionScope.Create();
        var measured = root.CreateChildScope();
        var failure = Assert.Throws<InvalidOperationException>(() => InvokeOperation(measured, source, operation, new CandidateMetric()));
        failure.Message.ShouldContain(name);
        failure.Message.ShouldContain("candidate budget");
        failure.Message.ShouldContain(nameof(AllOperations));
        source.Constructions.ShouldBe(0);
        // Rejection precedes the hook and all private data construction.
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
        var scope = ResolutionScope.Create();

        Assert.Throws<NotSupportedException>(() => InvokeOperation(scope, source, operation, new CandidateWorkWrapping(probe)));
        probe.Successes.ShouldBe(0);
        source.Invocations.ShouldBe(1);
    }

    [Fact]
    public void UnsupportedExternalScalarMetricRejectsBeforeItsChildIsResolved()
    {
        var child = new SharedOperations(new ProbeLog());
        var source = new Forward<Checksum, RealVector>(child);
        var scope = ResolutionScope.Create();
        var failure = Assert.Throws<InvalidOperationException>(() =>
            scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem).Resolve(new Wrapped<Checksum>(source, new CandidateMetric())));
        failure.Message.ShouldContain(nameof(Checksum));
        child.Log.Preparations.ShouldBe(0);
    }

    // The one hook's executor must serve each operation requested, and resolution says so whichever comes first.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnOperationTheHooksExecutorDoesNotServe_IsRejected(bool unservedFirst)
    {
        var source = new MutatorAddingEvaluation();
        var typed = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem);
        Action served = () => typed.Resolve((IOperator<Mutation, RealVector>)source);
        Action unserved = () => typed.Resolve((IOperator<Evaluation, RealVector>)source);

        if (!unservedFirst)
            served();
        Should.Throw<InvalidOperationException>(unserved).Message.ShouldContain(nameof(MutatorAddingEvaluation));
    }

    // Different problems allow one configuration to serve distinct candidate types in the same scope.
    [Fact]
    public void DifferentCandidatesOnDifferentProblems_ConstructTheirOwnExecutors()
    {
        var source = new TwoCandidateMutator();
        var scope = ResolutionScope.Create();
        scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem).Resolve((IOperator<Mutation, RealVector>)source);

        var integerProblem = FuncProblem.Create(static (int x) => x, HEAL.HeuristicLib.Tests.TestSupport.Mocks.DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        scope.For<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(integerProblem).Resolve((IOperator<Mutation, int>)source).ShouldNotBeNull();
    }

    private static IExecutor ResolveOperation(ResolutionScope scope, AllOperations source, int operation, Wrapping? wrapping = null)
    {
        var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>(Problem);
        IOperator<TOperation, RealVector> Wrap<TOperation>(IOperator<TOperation, RealVector> child)
            where TOperation : IOperation<TOperation> => wrapping is null ? child : new Wrapped<TOperation>(child, wrapping);
        return operation switch
        {
            0 => typed.ResolveOperator(Wrap((IOperator<Creation, RealVector>)source)),
            1 => typed.ResolveOperator(Wrap((IOperator<Crossover, RealVector>)source)),
            2 => typed.ResolveOperator(Wrap((IOperator<Mutation, RealVector>)source)),
            3 => typed.ResolveOperator(Wrap((IOperator<Evaluation, RealVector>)source)),
            4 => typed.ResolveOperator(Wrap((IOperator<Selection, RealVector>)source)),
            5 => typed.ResolveOperator(Wrap((IOperator<Refinement, RealVector>)source)),
            6 => typed.ResolveOperator(Wrap((IOperator<Replacement, RealVector>)source)),
            7 => typed.ResolveOperator(Wrap((IOperator<Termination, RealVector>)source)),
            8 => typed.ResolveOperator(Wrap((IOperator<Interception<ProbeState>, RealVector>)source)),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static void InvokeOperation(ResolutionScope scope, AllOperations source, int operation, Wrapping? wrapping = null, bool retry = false)
    {
        var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>(Problem);
        var random = RandomNumberGenerator.Create(1);
        var population = Candidates.ToEvaluated<RealVector>([1.0, 2.0, 3.0]);
        IOperator<TOperation, RealVector> Wrap<TOperation>(IOperator<TOperation, RealVector> child)
            where TOperation : IOperation<TOperation>
        {
            var wrapped = wrapping is null ? child : new Wrapped<TOperation>(child, wrapping);
            return retry ? wrapped.Retry(3) : wrapped;
        }
        switch (operation)
        {
            case 0:
                typed.Resolve(Wrap((IOperator<Creation, RealVector>)source)).Create(3, random).ShouldBeSameAs(AllOperations.Output);
                break;
            case 1:
                typed.Resolve(Wrap((IOperator<Crossover, RealVector>)source)).Cross([new(Candidates[0], Candidates[1])], random).ShouldBeSameAs(AllOperations.Output);
                break;
            case 2:
                typed.Resolve(Wrap((IOperator<Mutation, RealVector>)source)).Mutate(Candidates, random).ShouldBeSameAs(AllOperations.Output);
                break;
            case 3:
                typed.Resolve(Wrap((IOperator<Evaluation, RealVector>)source)).Evaluate(Candidates, random).ShouldBeEmpty();
                break;
            case 4:
                typed.Resolve(Wrap((IOperator<Selection, RealVector>)source)).Select(population, Problem.Objective, 3, random).ShouldBeSameAs(AllOperations.Population);
                break;
            case 5:
                typed.Resolve(Wrap((IOperator<Refinement, RealVector>)source)).Refine(Candidates, random).ShouldBeSameAs(AllOperations.Output);
                break;
            case 6:
                typed.Resolve(Wrap((IOperator<Replacement, RealVector>)source)).Replace(population, population, Problem.Objective, 3, random).ShouldBeSameAs(AllOperations.Population);
                break;
            case 7:
                typed.Resolve(Wrap((IOperator<Termination, RealVector>)source)).IsTerminalState(new ProbeState(), random).ShouldBeTrue();
                break;
            case 8:
                var state = new ProbeState();
                typed.Resolve(Wrap((IOperator<Interception<ProbeState>, RealVector>)source)).Transform(state, null, random).ShouldBeSameAs(state);
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

    // Wraps its child's executor for whichever operation it is resolved, as an external author writes one wrapper for every
    // operation. What the wrapper does comes from the test.
    private sealed record Wrapped<TOperation> : WrappingOperator<TOperation, RealVector>
        where TOperation : IOperation<TOperation>
    {
        public Wrapped(IOperator<TOperation, RealVector> childOperator, Wrapping wrapping)
            : base(childOperator)
        {
            wrapping.Check(childOperator);
            Wrapping = wrapping;
        }

        public Wrapping Wrapping { get; init; }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) =>
            Wrapping.Wrap(scope.ResolveOperator(ChildOperator), OperationInfo.For<TOperation, RealVector, TSearchState>());
    }

    // What a wrapper does around the executor of any operation, and what it rejects when it is placed.
    private abstract class Wrapping
    {
        public virtual void Check<TOperation>(IOperator<TOperation, RealVector> child) where TOperation : IOperation<TOperation> { }

        public abstract IExecutor Wrap(IExecutor executor, OperationInfo operation);
    }

    private sealed class ProbeWrapping(HookProbe probe) : Wrapping
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation) =>
            operation.Wrap(executor, new ProbeWrapper(probe, operation.SupportsCandidateWork));
    }

    // Asks for candidate work without checking the operation, which only a faulty wrapper does.
    private sealed class CandidateWorkWrapping(HookProbe probe) : Wrapping
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation)
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

    private sealed class TwiceWrapping : Wrapping
    {
        public int Wraps { get; private set; }

        public override IExecutor Wrap(IExecutor executor, OperationInfo operation)
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

    private sealed class RedirectingWrapping(IExecutor? target, IRandomNumberGenerator? random, InvocationRecorder recorder) : Wrapping
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation) =>
            operation.Wrap(executor, new RedirectingWrapper(target, random, recorder));
    }

    private sealed class SuppliedControl;

    private readonly struct ControlSupplyingWrapper(SuppliedControl control) : IInvocationWrapper, IExecutorControlProvider
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult> =>
            invocation.Proceed();

        public TControl? Get<TControl>() where TControl : class => control as TControl;
    }

    private sealed class ControlSupplyingWrapping(SuppliedControl control) : Wrapping
    {
        public override IExecutor Wrap(IExecutor executor, OperationInfo operation) =>
            operation.Wrap(executor, new ControlSupplyingWrapper(control));
    }

    // Rejects an operation without candidate work when it is placed, before anything is resolved.
    private sealed class CandidateMetric : Wrapping
    {
        public override void Check<TOperation>(IOperator<TOperation, RealVector> child)
        {
            if (!TOperation.SupportsCandidateWork)
                throw new InvalidOperationException($"{typeof(TOperation).Name} declares no candidate work, so a candidate budget cannot measure {child.GetType().Name}.");
        }

        public override IExecutor Wrap(IExecutor executor, OperationInfo operation) => executor;
    }

    // Inherits its mutator's hook, whose executor performs no evaluation.
    private sealed record MutatorAddingEvaluation : StatelessMutator<RealVector>, IOperator<Evaluation, RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) => parents;
    }

    // Implements the hook once per candidate, and each executor serves its own candidate only.
    private sealed class TwoCandidateMutator : IOperator<Mutation, RealVector>, IOperator<Mutation, int>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => new Executor<RealVector>();

        IExecutor IOperator<int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor<int>();

        private sealed class Executor<TCandidate> : IMutationExecutor<TCandidate>
        {
            public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) => parents;
        }
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
        // Each executor prepares private data during construction.
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            Preparations++;
            Constructions++;
            return new Executor(this);
        }

        private void Call(IRandomNumberGenerator random)
        {
            RandomSources.Add(random);
            Invocations++;
            if (Failure is { } failure && Invocations <= FailuresBeforeSuccess)
                throw failure;
        }

        private sealed class Executor(AllOperations source) :
            ICreationExecutor<RealVector>,
            ICrossoverExecutor<RealVector>,
            IMutationExecutor<RealVector>,
            IEvaluationExecutor<RealVector>,
            ISelectionExecutor<RealVector>,
            IRefinementExecutor<RealVector>,
            IReplacementExecutor<RealVector>,
            ITerminationExecutor<ISearchState>,
            IInterceptionExecutor<ProbeState>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random) { source.Call(random); return Output; }
            public IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random) { source.Call(random); return Output; }
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) { source.Call(random); return Output; }
            // Empty objective output deliberately distinguishes input work from result cardinality.
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) { source.Call(random); return []; }
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) { source.Call(random); return Population; }
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) { source.Call(random); return Output; }
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) { source.Call(random); return Population; }
            public bool IsTerminalState(ISearchState state, IRandomNumberGenerator random) { source.Call(random); return true; }
            public ProbeState Transform(ProbeState currentState, ProbeState? previousState, IRandomNumberGenerator random) { source.Call(random); return currentState; }
        }

    }
}
