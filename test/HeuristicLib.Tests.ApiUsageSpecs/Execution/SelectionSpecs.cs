using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public class SelectionSpecs
{
    [Fact]
    public void NamingWrappersUseTheirChildsExplicitAdaptationLink()
    {
        var adaptation = new ScaleAdaptation();
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5) { MutationStrengthAdaptation = adaptation };
        var exploration = new Named<Mutation, RealVector>(child, "exploration");
        var refinement = new Named<Mutation, RealVector>(child, "refinement");
        var problem = new TestFunctionProblem(new SphereFunction(3));
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);

        // Both names resolve their child in this scope, so its link resolves to the same adaptation.
        _ = scope.Resolve(exploration);
        _ = scope.Resolve(refinement);
        _ = scope.Resolve(child);
        scope.Resolve(adaptation).Factor *= 0.5;

        scope.Resolve(adaptation).Apply(child.MutationStrength).ShouldBe(0.25);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OneSubstitutionCanCountEveryConfiguredMutatorInAnAlgorithm(bool useGroup)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var exploration = new GaussianMutator(mutationRate: 0.5, mutationStrength: 1.0);
        var refinement = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1);
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 16,
            MaximumGenerations = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = ChoosePerItem.Create<Mutation, RealVector>(exploration, refinement),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.5,
            Elites = 1
        };
        var mutations = new Counter();
        ISubstitution counting = useGroup
            ? new MutationCounter(mutations)
            : Substitution.Of((IOperator<Mutation, RealVector> mutator) => mutator.Counted(mutations));

        var run = algorithm.Substitute(counting).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(mutations).CurrentCount.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConsumerDefinedRolesCanBeWrappedDirectly(bool selectByReference)
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var queriedTemperatures = new List<double>();

        var recording = selectByReference
            ? Substitution.Of(warmup, schedule => new RecordedCooling(schedule, queriedTemperatures.Add))
            : Substitution.Of((ICoolingSchedule schedule) => new RecordedCooling(schedule, queriedTemperatures.Add));
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve(plan.Substitute(recording));

        executor.Temperature(step: 0).ShouldBe(100);
        executor.Temperature(step: 5).ShouldBe(2);
        queriedTemperatures.ShouldBe(selectByReference ? [100] : [100, 2]);
    }

    [Fact]
    public void ConsumerDefinedRolesCanFeedATrace()
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var temperatures = Analyzer.Trace([Temperatures(warmup), Temperatures(warmup), Temperatures(main)],
            static temperature => temperature);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve(plan.Substitute(temperatures.Expand()));

        executor.Temperature(step: 0).ShouldBe(100);
        var first = scope.Results.Result(temperatures).Snapshot();
        executor.Temperature(step: 5).ShouldBe(2);

        first.Values.ShouldBe([100]);
        scope.Results.Result(temperatures).Snapshot().Values.ShouldBe([100, 2]);
    }

    [Fact]
    public void ConsumerDefinedRoleSourcesCanFeedACallback()
    {
        ICoolingSchedule schedule = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var values = new List<double>();
        var callback = Temperatures(schedule).Observing(values.Add);
        var plan = new AnnealingPlan(schedule, schedule, WarmupSteps: 3);
        var executor = ResolutionScope.Create().Resolve(plan.Substitute(callback));

        callback.Expand().Single().ShouldBeSameAs(callback.Expand().Single());
        executor.Temperature(step: 0).ShouldBe(8);
        executor.Temperature(step: 5).ShouldBe(2);
        values.ShouldBe([8, 2]);
    }

    // A role of your own is observed by substituting its own wrapper for it, or explicitly by writing that wrapper.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConsumerDefinedRolesAreObservedThroughAttachmentOrTheirOwnWrapper(bool attached)
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var temperatures = Analyzer.Trace([Temperatures(warmup), Temperatures(main)], static temperature => temperature);
        var plan = attached
            ? new AnnealingPlan(warmup, main, WarmupSteps: 3)
            : new AnnealingPlan(new ObservedCooling(warmup, temperatures), new ObservedCooling(main, temperatures), WarmupSteps: 3);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve(attached ? plan.Substitute(temperatures.Expand()) : plan);

        executor.Temperature(step: 0).ShouldBe(100);
        executor.Temperature(step: 5).ShouldBe(2);

        scope.Results.Result(temperatures).Snapshot().Values.ShouldBe([100, 2]);
    }

    // A later placement walks through the wrapper an earlier one left, so a role's wrapper declares what it holds.
    [Fact]
    public void ARoleCanBeObservedBySuccessivePlacements()
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var first = Analyzer.Trace([Temperatures(warmup)], static temperature => temperature);
        var second = Analyzer.Trace([Temperatures(warmup)], static temperature => temperature);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve(plan.Substitute(first.Expand()).Substitute(second.Expand()));

        executor.Temperature(step: 0).ShouldBe(100);

        scope.Results.Result(first).Snapshot().Values.ShouldBe([100]);
        scope.Results.Result(second).Snapshot().Values.ShouldBe([100]);
    }

    // A run reads a definition only when its algorithm holds it, which the role's wrapper makes visible by declaring it.
    [Fact]
    public async Task ARunReadsTheTraceOfARoleItsAlgorithmHolds()
    {
        ICoolingSchedule schedule = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        var temperatures = Analyzer.Trace([Temperatures(schedule)], static temperature => temperature);
        var algorithm = new CoolingAlgorithm(schedule).Substitute(temperatures);

        var run = algorithm.CreateRun(new TestFunctionProblem(new SphereFunction(1)), RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(temperatures).Snapshot().Values.ShouldBe([100]);
    }

    // The role's calls receive no problem, so its observations are the plain temperatures each call returned.
    private static ObservationSource<double> Temperatures(ICoolingSchedule schedule) =>
        ObservationSource.Create<ICoolingSchedule, double>(schedule, static (node, receiver) => new ObservedCooling(node, receiver));

    // A group of one's own: it counts every mutator the configuration it is applied to holds.
    private sealed class MutationCounter(Counter mutations) : SubstitutionGroup
    {
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
            [Substitution.Of((IOperator<Mutation, RealVector> mutator) => mutator.Counted(mutations))];
    }

    // A consumer-defined role: the library knows nothing about cooling schedules.
    private interface ICoolingSchedule : IConfigurationNode<ICoolingScheduleExecutor>;

    private interface ICoolingScheduleExecutor : IExecutor
    {
        double Temperature(int step);
    }

    private sealed record GeometricCooling(double StartTemperature, double Factor) : ICoolingSchedule
    {
        public ICoolingScheduleExecutor CreateExecutionNode(ResolutionScope scope) => new Executor(StartTemperature, Factor);

        private sealed class Executor(double startTemperature, double factor) : ICoolingScheduleExecutor
        {
            public double Temperature(int step) => startTemperature * Math.Pow(factor, step);
        }
    }

    // A wrapper of the role written outside the library, recording each temperature its schedule returns.
    private sealed record RecordedCooling(ICoolingSchedule Schedule, Action<double> Record) : ICoolingSchedule
    {
        public static Parameter<RecordedCooling, ICoolingSchedule> ScheduleParameter { get; } =
            new(nameof(Schedule), static node => node.Schedule, static (node, value) => node with { Schedule = value });

        public IReadOnlyList<IParameter> GetParameters() => [ScheduleParameter];

        public ICoolingScheduleExecutor CreateExecutionNode(ResolutionScope scope) => new RecordingExecutor(scope.Resolve(Schedule), Record);
    }

    private sealed class RecordingExecutor(ICoolingScheduleExecutor schedule, Action<double> record) : ICoolingScheduleExecutor
    {
        public double Temperature(int step)
        {
            var temperature = schedule.Temperature(step);
            record(temperature);
            return temperature;
        }
    }

    // The role's own observation wrapper, recording each temperature into the receiver.
    private sealed record ObservedCooling(ICoolingSchedule Schedule, IObservationReceiver<double> Receiver) : ICoolingSchedule
    {
        public static Parameter<ObservedCooling, ICoolingSchedule> ScheduleParameter { get; } =
            new(nameof(Schedule), static node => node.Schedule, static (node, value) => node with { Schedule = value });

        public static Parameter<ObservedCooling, IObservationReceiver<double>> ReceiverParameter { get; } =
            new(nameof(Receiver), static node => node.Receiver, static (node, value) => node with { Receiver = value });

        public IReadOnlyList<IParameter> GetParameters() => [ScheduleParameter, ReceiverParameter];

        public ICoolingScheduleExecutor CreateExecutionNode(ResolutionScope scope) =>
            new RecordingExecutor(scope.Resolve(Schedule), Receiver.CreateRecorder(scope.Results));
    }

    private sealed record CoolingAlgorithm(ICoolingSchedule Schedule)
        : Algorithm<CoolingAlgorithm, RealVector, SingleSolutionState<RealVector>>
    {
        public static Parameter<CoolingAlgorithm, ICoolingSchedule> ScheduleParameter { get; } =
            new(nameof(Schedule), static algorithm => algorithm.Schedule, static (algorithm, value) => algorithm with { Schedule = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), ScheduleParameter];

        protected override IAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope) =>
            new Executor(scope.Resolve(Schedule));

        private sealed class Executor(ICoolingScheduleExecutor schedule)
            : AlgorithmExecutor<SingleSolutionState<RealVector>>
        {
            public override async IAsyncEnumerable<SingleSolutionState<RealVector>> RunStreamingAsync(IRandomNumberGenerator random, SingleSolutionState<RealVector>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                yield return SingleSolutionState.From(new RealVector(schedule.Temperature(step: 0)), new ObjectiveVector(0));
                await Task.CompletedTask;
            }
        }
    }

    private sealed record AnnealingPlan(ICoolingSchedule Warmup, ICoolingSchedule Main, int WarmupSteps)
        : IConfigurationNode<AnnealingPlan.Executor>
    {
        public static Parameter<AnnealingPlan, ICoolingSchedule> WarmupParameter { get; } =
            new(nameof(Warmup), static node => node.Warmup, static (node, value) => node with { Warmup = value });

        public static Parameter<AnnealingPlan, ICoolingSchedule> MainParameter { get; } =
            new(nameof(Main), static node => node.Main, static (node, value) => node with { Main = value });

        public IReadOnlyList<IParameter> GetParameters() => [WarmupParameter, MainParameter];

        public Executor CreateExecutionNode(ResolutionScope scope) => new(scope.Resolve(Warmup), scope.Resolve(Main), WarmupSteps);

        public sealed class Executor(ICoolingScheduleExecutor warmup, ICoolingScheduleExecutor main, int warmupSteps) : IExecutor
        {
            public double Temperature(int step) =>
                step < warmupSteps ? warmup.Temperature(step) : main.Temperature(step - warmupSteps);
        }
    }
}
