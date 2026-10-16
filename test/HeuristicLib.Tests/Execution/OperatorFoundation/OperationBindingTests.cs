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
    public void EveryDescriptorPreservesItsDomainCallAndReportsDeclaredWork(int operation, int expectedWork)
    {
        foreach (var lifecycle in new[] { false, true })
        {
            var source = new AllOperations();
            var probe = new HookProbe();
            var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new ProbeAdvice(probe, lifecycle)));
            Invoke(scope, source, operation);
            probe.Work.ShouldBe(expectedWork);
            probe.Successes.ShouldBe(1);
            probe.Cleanups.ShouldBe(lifecycle ? 1 : 0);
            source.Calls.ShouldBe(1);
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
    public void EveryDescriptorReportsFaultAndCleansUpOnce(int operation)
    {
        var failure = new OperationCanceledException("operation canceled");
        var source = new AllOperations { Failure = failure };
        var probe = new HookProbe();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new ProbeAdvice(probe, true)));
        Assert.Throws<OperationCanceledException>(() => Invoke(scope, source, operation)).ShouldBeSameAs(failure);
        probe.Failure.ShouldBeSameAs(failure);
        probe.Successes.ShouldBe(0);
        probe.Cleanups.ShouldBe(1);
        source.Calls.ShouldBe(1);
    }

    [Theory]
    [InlineData(7, "Termination")]
    [InlineData(8, "Interception")]
    public void CandidateMetricRejectionPrecedesConstructionAndPreservesTheOwner(int operation, string name)
    {
        var source = new AllOperations();
        var root = ResolutionScope.Create();
        var measured = root.With(Aspect.Create(Pointcut.Reference(source), new CandidateMetric()));
        var failure = Assert.Throws<InvalidOperationException>(() => Invoke(measured, source, operation));
        failure.Message.ShouldContain(name);
        failure.Message.ShouldContain("candidate budget");
        failure.Message.ShouldContain(nameof(AllOperations));
        source.Constructions.ShouldBe(0);
        source.Preparations.ShouldBe(1);
        Invoke(root, source, operation);
        source.Constructions.ShouldBe(1);
        source.Preparations.ShouldBe(1);
    }

    [Fact]
    public void UnsupportedExternalScalarMetricRejectsBeforeItsChildIsResolved()
    {
        var child = new SharedOperations(new ProbeLog());
        var source = new Forward<RealVector, Checksum>(child);
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CandidateMetric()));
        var failure = Assert.Throws<InvalidOperationException>(() =>
            scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source));
        failure.Message.ShouldContain(nameof(Checksum));
        child.Log.Preparations.ShouldBe(0);
    }

    private static void Invoke(ResolutionScope scope, AllOperations source, int operation)
    {
        var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ProbeState>();
        var random = RandomNumberGenerator.Create(1);
        var space = Problem.SearchSpace;
        var population = Candidates.ToEvaluated<RealVector>([1.0, 2.0, 3.0]);
        switch (operation)
        {
            case 0:
                typed.Resolve((IOperator<RealVector, Creation>)source).Create(3, random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 1:
                typed.Resolve((IOperator<RealVector, Crossover>)source).Cross([new(Candidates[0], Candidates[1])], random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 2:
                typed.Resolve((IOperator<RealVector, Mutation>)source).Mutate(Candidates, random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 3:
                typed.Resolve((IOperator<RealVector, Evaluation>)source).Evaluate(Candidates, random, space, Problem).ShouldBeEmpty();
                break;
            case 4:
                typed.Resolve((IOperator<RealVector, Selection>)source).Select(population, Problem.Objective, 3, random, space, Problem).ShouldBeSameAs(AllOperations.Population);
                break;
            case 5:
                typed.Resolve((IOperator<RealVector, Refinement>)source).Refine(Candidates, random, space, Problem).ShouldBeSameAs(AllOperations.Output);
                break;
            case 6:
                typed.Resolve((IOperator<RealVector, Replacement>)source).Replace(population, population, Problem.Objective, 3, random, space, Problem).ShouldBeSameAs(AllOperations.Population);
                break;
            case 7:
                typed.Resolve((IOperator<RealVector, Termination>)source).IsTerminalState(new ProbeState(), space, Problem).ShouldBeTrue();
                break;
            case 8:
                var state = new ProbeState();
                typed.Resolve((IOperator<RealVector, Interception>)source).Transform(state, null, random, space, Problem).ShouldBeSameAs(state);
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

    private readonly struct Hooks(HookProbe probe) : IOperationSuccessCallback, IOperationCallCallbacks<long>
    {
        public long OnStarting() => 17;
        public void OnSucceeded(int candidateWork) { probe.Work = candidateWork; probe.Successes++; }
        public void OnSucceeded(long token, int candidateWork) { token.ShouldBe(17); OnSucceeded(candidateWork); }
        public void OnFailed(long token, Exception failure) { token.ShouldBe(17); probe.Failure = failure; }
        public void OnFinished(long token) { token.ShouldBe(17); probe.Cleanups++; }
    }

    private sealed class ProbeAdvice(HookProbe probe, bool lifecycle) : OperatorAdvice
    {
        public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(probe, lifecycle);

        private sealed class WrapperFactory(HookProbe probe, bool lifecycle) : OperatorAdviceWrapperFactory
        {
            public override WrapperExecutorFactory GetWrapperFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
                IOperator<TCandidate, TOperation> source,
                OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter) =>
                lifecycle ? adapter.Hooks<Hooks, long>(new Hooks(probe)) : adapter.Success(new Hooks(probe));
        }
    }

    private sealed class CandidateMetric : OperatorAdvice
    {
        public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory();

        private sealed class WrapperFactory : OperatorAdviceWrapperFactory
        {
            public override WrapperExecutorFactory GetWrapperFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
                IOperator<TCandidate, TOperation> source,
                OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter)
            {
                adapter.RequireCandidateWork(source, "candidate budget");
                return adapter.Identity();
            }
        }
    }

    private sealed class AllOperations : IOperator<RealVector, Creation>, IOperator<RealVector, Crossover>,
        IOperator<RealVector, Mutation>, IOperator<RealVector, Evaluation>, IOperator<RealVector, Selection>,
        IOperator<RealVector, Refinement>, IOperator<RealVector, Replacement>, IOperator<RealVector, Termination>,
        IOperator<RealVector, Interception>
    {
        public static readonly RealVector[] Output = [new(4.0), new(5.0)];
        public static readonly IReadOnlyList<EvaluatedCandidate<RealVector>> Population = Output.ToEvaluated<RealVector>([4.0, 5.0]);
        public int Preparations { get; private set; }
        public int Constructions { get; private set; }
        public int Calls { get; private set; }
        public Exception? Failure { get; init; }

        public OperatorExecutorFactory CreateExecutorFactory()
        {
            Preparations++;
            var executor = new Executor(this);
            return OperatorExecutorFactory.FromFactory<Executor>(_ => { Constructions++; return executor; });
        }

        private void Call()
        {
            Calls++;
            if (Failure is { } failure)
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
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return Output; }
            public IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return Output; }
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return Output; }
            // Empty objective output deliberately distinguishes input work from result cardinality.
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return []; }
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return Population; }
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return Output; }
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return Population; }
            public bool IsTerminalState(ISearchState state, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return true; }
            public ProbeState Transform(ProbeState currentState, ProbeState? previousState, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) { source.Call(); return currentState; }
        }
    }
}
