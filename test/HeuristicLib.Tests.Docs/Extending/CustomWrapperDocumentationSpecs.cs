#region custom-wrapper-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
#endregion custom-wrapper-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region custom-wrapper-type
public sealed record LoggedMutator<TCandidate> : WrappingOperator<Mutation, TCandidate>
{
    public LoggedMutator(IOperator<Mutation, TCandidate> childMutator, Action<IOperator<Mutation, TCandidate>, int, int> write)
        : base(childMutator)
    {
        Write = write;
    }

    public Action<IOperator<Mutation, TCandidate>, int, int> Write { get; init; }

    public static Parameter<LoggedMutator<TCandidate>, Action<IOperator<Mutation, TCandidate>, int, int>> WriteParameter { get; } =
        new(nameof(Write), static wrapper => wrapper.Write, static (wrapper, value) => wrapper with { Write = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [WriteParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new LoggingExecutor(scope.Resolve(ChildOperator), ChildOperator, Write);

    private sealed class LoggingExecutor(
        IMutationExecutor<TCandidate> executor,
        IOperator<Mutation, TCandidate> mutator,
        Action<IOperator<Mutation, TCandidate>, int, int> write)
        : IMutationExecutor<TCandidate>
    {
        private int calls;

        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random)
        {
            var offspring = executor.Mutate(parents, random);
            write(mutator, ++calls, offspring.Count);
            return offspring;
        }
    }
}
#endregion custom-wrapper-type

#region general-wrapper-type
public sealed record SlowCallReporter<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    public SlowCallReporter(IOperator<TOperation, TCandidate> childOperator, TimeSpan threshold,
        Action<IOperator, TimeSpan> report)
        : base(childOperator)
    {
        Threshold = threshold;
        Report = report;
    }

    public TimeSpan Threshold { get; init; }
    public Action<IOperator, TimeSpan> Report { get; init; }

    public static Parameter<SlowCallReporter<TOperation, TCandidate>, TimeSpan> ThresholdParameter { get; } =
        new(nameof(Threshold), static wrapper => wrapper.Threshold, static (wrapper, value) => wrapper with { Threshold = value });

    public static Parameter<SlowCallReporter<TOperation, TCandidate>, Action<IOperator, TimeSpan>> ReportParameter { get; } =
        new(nameof(Report), static wrapper => wrapper.Report, static (wrapper, value) => wrapper with { Report = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [ThresholdParameter, ReportParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var operation = OperationInfo.For<TOperation, TCandidate, TSearchState>();
        return operation.Wrap(scope.ResolveOperator(ChildOperator), new ReportSlowCalls(Threshold, scope.Results.TimeProvider, Report, ChildOperator));
    }

    private readonly struct ReportSlowCalls(TimeSpan threshold, TimeProvider time, Action<IOperator, TimeSpan> report,
        IOperator source) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            var start = time.GetTimestamp();
            var result = invocation.Proceed();
            var elapsed = time.GetElapsedTime(start);
            if (elapsed >= threshold)
                report(source, elapsed);
            return result;
        }
    }
}
#endregion general-wrapper-type

public sealed class CustomWrapperDocumentationSpecs
{
    [Fact]
    public async Task AWrapperLogsTheMutatorItWrapsInARun()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region custom-wrapper-run
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create(
            (RealVector candidate) => candidate[0] * candidate[0],
            searchSpace,
            SingleObjective.Minimize);
        var mutator = new NoChangeMutator<RealVector>();
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = mutator,
            PopulationSize = 4,
            MutationRate = 1.0,
            MaximumGenerations = 3
        };
        var messages = new List<string>();
        var logged = algorithm.Substitute(mutator, child => new LoggedMutator<RealVector>(child, (source, call, count) =>
            messages.Add($"{source.GetType().Name}: call {call}, {count} offspring")));

        var run = logged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion custom-wrapper-run

        messages.Count.ShouldBe(2);
        messages[0].ShouldBe("NoChangeMutator`1: call 1, 4 offspring");
        messages[1].ShouldBe("NoChangeMutator`1: call 2, 4 offspring");
    }

    [Fact]
    public void EachExecutorOfTheWrapperNumbersItsOwnCalls()
    {
        var mutator = new NoChangeMutator<RealVector>();
        var calls = new List<int>();
        var logged = new LoggedMutator<RealVector>(mutator, (_, call, _) => calls.Add(call));
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(42);
        IReadOnlyList<RealVector> parents = [new RealVector(0.0)];
        var scope = ResolutionScope.Create();
        var original = scope.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(logged);
        var repeated = scope.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(logged);
        var rebound = scope.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(logged);

        original.Mutate(parents, random);
        repeated.Mutate(parents, random);
        rebound.Mutate(parents, random);
        var independent = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(logged);
        independent.Mutate(parents, random);

        calls.ShouldBe([1, 2, 1, 1]);
    }

    [Fact]
    public async Task AGeneralWrapperWrapsEveryOperationItIsPlacedOn()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            PopulationSize = 4,
            MutationRate = 1.0,
            MaximumGenerations = 3
        };

        #region general-wrapper-run
        var slow = new List<string>();
        Action<IOperator, TimeSpan> report = (source, elapsed) => slow.Add(source.GetType().Name);
        var reporting = algorithm.Substitute(
            Substitution.Of(algorithm.Mutator, mutator =>
                new SlowCallReporter<Mutation, RealVector>(mutator, TimeSpan.Zero, report)),
            Substitution.Of(algorithm.Evaluator, evaluator =>
                new SlowCallReporter<Evaluation, RealVector>(evaluator, TimeSpan.Zero, report)));
        var run = reporting.CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        #endregion general-wrapper-run
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        slow.ShouldContain(algorithm.Mutator.GetType().Name);
        slow.ShouldContain(algorithm.Evaluator.GetType().Name);
    }
}
