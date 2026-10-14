using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public class PointcutSpecs
{
    [Fact]
    public void AReferenceRetainsItsDeclaredRole()
    {
        IMutator<RealVector> mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        Pointcut<IMutator<RealVector>> selected = Pointcut.Reference(mutator);

        selected.Matches(mutator).ShouldBeTrue();
        selected.Matches(new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5)).ShouldBeFalse();
    }

    [Fact]
    public void RoleAndConcreteTypeSelectionsSupportConfigurationFilters()
    {
        var smallMutation = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var largeMutation = smallMutation with { MutationStrength = 2.0 };

        Pointcut<IMutator<RealVector>> selectedMutators = Pointcut.OfType<IMutator<RealVector>>()
            .And(new SmallMutations());
        Pointcut<GaussianMutator> selectedGaussianMutators = Pointcut.OfType<GaussianMutator>()
            .And(new SmallGaussianMutations());

        selectedMutators.Matches(smallMutation).ShouldBeTrue();
        selectedGaussianMutators.Matches(smallMutation).ShouldBeTrue();
        selectedMutators.Matches(largeMutation).ShouldBeFalse();
        selectedGaussianMutators.Matches(largeMutation).ShouldBeFalse();
    }

    [Fact]
    public void NamesCanBePropertiesOnConsumerAuthoredWrappers()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var exploration = new NamedMutator<RealVector>(child, "exploration");
        var refinement = new NamedMutator<RealVector>(child, "refinement");
        var allMutators = Pointcut.OfType<IMutator<RealVector>>();
        Pointcut<IMutator<RealVector>> selected = allMutators
            .And(new ExplorationMutations());

        // Matches examines supplied nodes; an aspect applies a selection while resolving.
        IConfigurationNode[] nodes = [child, exploration, refinement];
        nodes.Where(selected.Matches).ShouldBe([exploration]);
        nodes.Where(allMutators.Matches).ShouldBe(nodes);
    }

    [Fact]
    public void NamingWrappersCanRetainTheirSharedChildExecution()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var exploration = new NamedMutator<RealVector>(child, "exploration");
        var refinement = new NamedMutator<RealVector>(child, "refinement");
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        var explorationExecution = scope.Resolve(exploration);
        var refinementExecution = scope.Resolve(refinement);

        explorationExecution.ShouldBeSameAs(refinementExecution);
        scope.Resolve(child).ShouldBeSameAs(explorationExecution);
    }

    [Fact]
    public void ReferenceAndRoleSelectionsComposeWithoutLosingTheRole()
    {
        IMutator<RealVector> preferred = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        IMutator<RealVector> alternative = new GaussianMutator(mutationRate: 0.2, mutationStrength: 2.0);
        var exactReference = Pointcut.Reference(preferred);
        var smallMutations = Pointcut.OfType<IMutator<RealVector>>()
            .And(new SmallMutations());

        Pointcut<IMutator<RealVector>> selected = exactReference.Or(smallMutations);
        Pointcut<IMutator<RealVector>> sameSelection = exactReference | smallMutations;
        IConfigurationNode[] nodes = [preferred, alternative];

        nodes.Where(selected.Matches).ShouldBe([preferred]);
        nodes.Where(sameSelection.Matches).ShouldBe([preferred]);
    }

    [Fact]
    public void APropertyConditionCanBeReusedAsASelector()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        IMutator<RealVector> exploration = new NamedMutator<RealVector>(child, "exploration");
        IMutator<RealVector> refinement = new NamedMutator<RealVector>(child, "refinement");
        var namedExploration = new ExplorationMutations();

        var namedReferences = Pointcut.Reference(exploration) | Pointcut.Reference(refinement);
        Pointcut<IMutator<RealVector>> selected = namedReferences & namedExploration;
        var sameSelection = namedReferences.And(new ExplorationMutations());
        IConfigurationNode[] nodes = [child, exploration, refinement];

        nodes.Where(selected.Matches).ShouldBe([exploration]);
        nodes.Where(sameSelection.Matches).ShouldBe([exploration]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OneAspectCanCountEveryConfiguredMutatorInAnAlgorithm(bool useSubclass)
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
            Mutator = ChooseOneMutator.Create<RealVector>(exploration, refinement),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.5,
            Elites = 1
        };
        var mutations = new CountAccumulator();
        var counting = useSubclass
            ? new MutationCounter(mutations)
            : Aspect.Create(Pointcut.OfType<IMutator<RealVector>>(), new CountingMutatorAdvice<RealVector>(mutations, OperatorCountMetric.Calls));

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .AddAspect(counting);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        mutations.CurrentCount.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConsumerDefinedRolesCanBeAdvisedDirectly(bool selectByReference)
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var queriedTemperatures = new List<double>();

        var recording = selectByReference
            ? Aspect.Create(Pointcut.Reference(warmup), new RecordedCoolingAdvice(queriedTemperatures.Add))
            : Aspect.Create(Pointcut.OfType<ICoolingSchedule>(), new RecordedCoolingAdvice(queriedTemperatures.Add));
        var scope = ResolutionScope.Create(recording);
        var execution = scope.Resolve(plan);

        execution.Temperature(step: 0).ShouldBe(100);
        execution.Temperature(step: 5).ShouldBe(2);
        queriedTemperatures.ShouldBe(selectByReference ? [100] : [100, 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneAspectCanBeUsedDirectlyOrSharedByIndependentCompositions(bool useCompositions)
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var queriedTemperatures = new List<double>();
        var recording = Aspect.Create(
            Pointcut.OfType<ICoolingSchedule>(),
            new RecordedCoolingAdvice(queriedTemperatures.Add));
        var first = new AspectGroup(recording);
        var second = new AspectGroup(recording);

        var scope = useCompositions ? ResolutionScope.Create(recording, first, second) : ResolutionScope.Create(recording);
        var execution = scope.Resolve(plan);
        execution.Temperature(step: 0).ShouldBe(100);
        execution.Temperature(step: 5).ShouldBe(2);

        queriedTemperatures.ShouldBe([100, 2]);
    }

    [Fact]
    public void OneInstallationApiCombinesExistingAspectsReferencesAndPointcuts()
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var events = new List<string>();
        var recording = Aspect.Create(Pointcut.OfType<ICoolingSchedule>(),
            new RecordedCoolingAdvice(temperature => events.Add($"existing:{temperature}")));
        Action<double> recordWarmup = temperature => events.Add($"warmup:{temperature}");
        Action<double> recordAll = temperature => events.Add($"all:{temperature}");
        var combined = new AspectGroup(recording,
            Aspect.Create(Pointcut.Reference(warmup), new RecordedCoolingAdvice(recordWarmup)),
            Aspect.Create(Pointcut.OfType<ICoolingSchedule>(), new RecordedCoolingAdvice(recordAll)));

        var execution = ResolutionScope.Create(combined).Resolve(plan);
        execution.Temperature(step: 0).ShouldBe(100);
        execution.Temperature(step: 5).ShouldBe(2);

        events.ShouldBe(["existing:100", "warmup:100", "all:100", "existing:2", "all:2"]);
    }

    [Fact]
    public void ConsumerDefinedRolesCanFeedATrace()
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var temperatures = TraceAnalyzer<double>.Create<ICoolingSchedule, double>(
            [warmup, warmup, main],
            static (schedule, record) => Aspect.Create(
                Pointcut.Reference(schedule), new RecordedCoolingAdvice(record)),
            static temperature => temperature);
        var execution = ResolutionScope.Create(temperatures).Resolve(plan);

        execution.Temperature(step: 0).ShouldBe(100);
        var first = temperatures.Snapshot();
        execution.Temperature(step: 5).ShouldBe(2);

        first.Values.ShouldBe([100]);
        temperatures.Snapshot().Values.ShouldBe([100, 2]);
    }

    private sealed class MutationCounter : Aspect
    {
        private static readonly Pointcut<IMutator<RealVector>> Mutators = Pointcut.OfType<IMutator<RealVector>>();

        public MutationCounter(CountAccumulator mutations)
        {
            Advices = [(Mutators, new CountingMutatorAdvice<RealVector>(mutations, OperatorCountMetric.Calls))];
        }
    }

    private sealed class SmallMutations : Pointcut<IMutator<RealVector>>
    {
        public override bool Matches(IMutator<RealVector> mutator) => mutator is GaussianMutator { MutationStrength: < 1.0 };
    }

    private sealed class SmallGaussianMutations : Pointcut<GaussianMutator>
    {
        public override bool Matches(GaussianMutator mutator) => mutator.MutationStrength < 1.0;
    }

    private sealed class ExplorationMutations : Pointcut<IMutator<RealVector>>
    {
        public override bool Matches(IMutator<RealVector> mutator) => mutator is NamedMutator<RealVector> { Name: "exploration" };
    }

    private sealed class AspectGroup : Aspect
    {
        public AspectGroup(params IReadOnlyList<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect.Advices)];
        }
    }

    // A consumer-defined role: the library knows nothing about cooling schedules.
    private interface ICoolingSchedule : IConfigurationNode<ICoolingScheduleExecution>;

    private interface ICoolingScheduleExecution : IExecutionNode
    {
        double Temperature(int step);
    }

    private sealed record GeometricCooling(double StartTemperature, double Factor) : ICoolingSchedule
    {
        public ExecutionFactory<ICoolingScheduleExecution> CreateExecutionFactory()
        {
            var execution = new Execution(StartTemperature, Factor);
            return _ => execution;
        }

        private sealed class Execution(double startTemperature, double factor) : ICoolingScheduleExecution
        {
            public double Temperature(int step) => startTemperature * Math.Pow(factor, step);
        }
    }

    private static WrapperExecutionFactory<ICoolingScheduleExecution> PrepareCoolingAdvice(Advice advice, ICoolingSchedule schedule) =>
        ((RecordedCoolingAdvice)advice).CreateWrapperFactory(schedule);

    private sealed class RecordedCoolingAdvice(Action<double> record) : Advice<ICoolingSchedule>
    {
        public WrapperExecutionFactory<ICoolingScheduleExecution> CreateWrapperFactory(ICoolingSchedule schedule) =>
            inner => new Execution(inner, record);

        private sealed class Execution(ICoolingScheduleExecution schedule, Action<double> record) : ICoolingScheduleExecution
        {
            public double Temperature(int step)
            {
                var temperature = schedule.Temperature(step);
                record(temperature);
                return temperature;
            }
        }
    }

    private sealed record AnnealingPlan(ICoolingSchedule Warmup, ICoolingSchedule Main, int WarmupSteps)
        : IConfigurationNode<AnnealingPlan.Execution>
    {
        public ExecutionFactory<Execution> CreateExecutionFactory() =>
            scope => new Execution(scope.Resolve(Warmup, static source => source.CreateExecutionFactory(), PrepareCoolingAdvice), scope.Resolve(Main, static source => source.CreateExecutionFactory(), PrepareCoolingAdvice), WarmupSteps);

        public sealed class Execution(ICoolingScheduleExecution warmup, ICoolingScheduleExecution main, int warmupSteps) : IExecutionNode
        {
            public double Temperature(int step) =>
                step < warmupSteps ? warmup.Temperature(step) : main.Temperature(step - warmupSteps);
        }
    }

    // This consumer-authored example is not a new library naming contract.
    private sealed record NamedMutator<TCandidate> : WrappingMutator<TCandidate>
    {
        public NamedMutator(IMutator<TCandidate> childMutator, string name)
            : base(childMutator)
        {
            Name = name;
        }

        public string Name { get; init; }

        protected override WrapperExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() => childMutator => childMutator;
    }
}
