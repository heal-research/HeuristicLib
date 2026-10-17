using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public class PointcutSpecs
{
    [Fact]
    public void AReferenceRetainsItsDeclaredRole()
    {
        IOperator<Mutation, RealVector> mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        Pointcut<IOperator<Mutation, RealVector>> selected = Pointcut.Reference(mutator);

        selected.Matches(mutator).ShouldBeTrue();
        selected.Matches(new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5)).ShouldBeFalse();
    }

    [Fact]
    public void RoleAndConcreteTypeSelectionsSupportConfigurationFilters()
    {
        var smallMutation = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var largeMutation = smallMutation with { MutationStrength = 2.0 };

        Pointcut<IOperator<Mutation, RealVector>> selectedMutators = Pointcut.Predicate<IOperator<Mutation, RealVector>>(
            mutator => mutator is GaussianMutator { MutationStrength: < 1 });
        Pointcut<GaussianMutator> selectedGaussianMutators = Pointcut.Predicate<GaussianMutator>(
            mutator => mutator.MutationStrength < 1);

        selectedMutators.Matches(smallMutation).ShouldBeTrue();
        selectedGaussianMutators.Matches(smallMutation).ShouldBeTrue();
        selectedMutators.Matches(largeMutation).ShouldBeFalse();
        selectedGaussianMutators.Matches(largeMutation).ShouldBeFalse();
    }

    [Fact]
    public void NamesCanBePropertiesOnConfiguredWrappers()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var exploration = new Named<Mutation, RealVector>(child, "exploration");
        var refinement = new Named<Mutation, RealVector>(child, "refinement");
        var allMutators = Pointcut.OfType<IOperator<Mutation, RealVector>>();
        Pointcut<IOperator<Mutation, RealVector>> selected = allMutators
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
        var exploration = new Named<Mutation, RealVector>(child, "exploration");
        var refinement = new Named<Mutation, RealVector>(child, "refinement");
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        var explorationExecutor = scope.Resolve(exploration);
        var refinementExecutor = scope.Resolve(refinement);

        explorationExecutor.ShouldBeSameAs(refinementExecutor);
        scope.Resolve(child).ShouldBeSameAs(explorationExecutor);
    }

    [Fact]
    public void NamedPointcutsCanExcludeOneOfSeveralNodesWithTheSameName()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var first = child.Named("exploration");
        var second = Named.Create(child, "exploration");
        var refinement = new Named<Mutation, RealVector>(child, "refinement");
        Pointcut<IOperator<Mutation, RealVector>> selected = Pointcut.Named<IOperator<Mutation, RealVector>>("exploration") &
            !Pointcut.Reference<IOperator<Mutation, RealVector>>(first);
        IConfigurationNode[] nodes = [child, first, second, refinement];

        var matches = nodes.Where(selected.Matches).ToArray();

        matches.Single().ShouldBeSameAs(second);
    }

    [Fact]
    public void ReferenceAndRoleSelectionsComposeWithoutLosingTheRole()
    {
        IOperator<Mutation, RealVector> preferred = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        IOperator<Mutation, RealVector> alternative = new GaussianMutator(mutationRate: 0.2, mutationStrength: 2.0);
        var exactReference = Pointcut.Reference(preferred);
        var smallMutations = Pointcut.OfType<IOperator<Mutation, RealVector>>()
            .And(new SmallMutations());

        Pointcut<IOperator<Mutation, RealVector>> selected = exactReference.Or(smallMutations);
        Pointcut<IOperator<Mutation, RealVector>> sameSelection = exactReference | smallMutations;
        IConfigurationNode[] nodes = [preferred, alternative];

        nodes.Where(selected.Matches).ShouldBe([preferred]);
        nodes.Where(sameSelection.Matches).ShouldBe([preferred]);
    }

    [Fact]
    public void APropertyConditionCanBeReusedAsASelector()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        IOperator<Mutation, RealVector> exploration = new Named<Mutation, RealVector>(child, "exploration");
        IOperator<Mutation, RealVector> refinement = new Named<Mutation, RealVector>(child, "refinement");
        var namedExploration = new ExplorationMutations();

        var namedReferences = Pointcut.Reference(exploration) | Pointcut.Reference(refinement);
        Pointcut<IOperator<Mutation, RealVector>> selected = namedReferences & namedExploration;
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
            : Aspect.Create(Pointcut.OfType<IOperator<Mutation, RealVector>>(), new CountingOperatorAdvice<Mutation, RealVector>(mutations, OperatorCountMetric.Calls));

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
        var executor = scope.Resolve(plan);

        executor.Temperature(step: 0).ShouldBe(100);
        executor.Temperature(step: 5).ShouldBe(2);
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
        var executor = scope.Resolve(plan);
        executor.Temperature(step: 0).ShouldBe(100);
        executor.Temperature(step: 5).ShouldBe(2);

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

        var executor = ResolutionScope.Create(combined).Resolve(plan);
        executor.Temperature(step: 0).ShouldBe(100);
        executor.Temperature(step: 5).ShouldBe(2);

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
        var executor = ResolutionScope.Create(temperatures).Resolve(plan);

        executor.Temperature(step: 0).ShouldBe(100);
        var first = temperatures.Snapshot();
        executor.Temperature(step: 5).ShouldBe(2);

        first.Values.ShouldBe([100]);
        temperatures.Snapshot().Values.ShouldBe([100, 2]);
    }

    private sealed class MutationCounter : Aspect
    {
        private static readonly Pointcut<IOperator<Mutation, RealVector>> Mutators = Pointcut.OfType<IOperator<Mutation, RealVector>>();

        public MutationCounter(CountAccumulator mutations)
        {
            Advices = [(Mutators, new CountingOperatorAdvice<Mutation, RealVector>(mutations, OperatorCountMetric.Calls))];
        }
    }

    private sealed class SmallMutations : Pointcut<IOperator<Mutation, RealVector>>
    {
        public override bool Matches(IOperator<Mutation, RealVector> mutator) => mutator is GaussianMutator { MutationStrength: < 1.0 };
    }

    private sealed class ExplorationMutations : Pointcut<IOperator<Mutation, RealVector>>
    {
        public override bool Matches(IOperator<Mutation, RealVector> mutator) => mutator is Named<Mutation, RealVector> { Name: "exploration" };
    }

    private sealed class AspectGroup : Aspect
    {
        public AspectGroup(params IReadOnlyList<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect.Advices)];
        }
    }

    // A consumer-defined role: the library knows nothing about cooling schedules.
    private interface ICoolingSchedule : IConfigurationNode<ICoolingScheduleExecutor>;

    private interface ICoolingScheduleExecutor : IExecutor
    {
        double Temperature(int step);
    }

    private sealed record GeometricCooling(double StartTemperature, double Factor) : ICoolingSchedule
    {
        public ExecutorFactory<ICoolingScheduleExecutor> CreateExecutorFactory()
        {
            var executor = new Executor(StartTemperature, Factor);
            return _ => executor;
        }

        private sealed class Executor(double startTemperature, double factor) : ICoolingScheduleExecutor
        {
            public double Temperature(int step) => startTemperature * Math.Pow(factor, step);
        }
    }

    private static WrapperExecutorFactory<ICoolingScheduleExecutor> PrepareCoolingAdvice(Advice advice, ICoolingSchedule schedule) =>
        ((RecordedCoolingAdvice)advice).CreateWrapperFactory(schedule);

    private sealed class RecordedCoolingAdvice(Action<double> record) : Advice<ICoolingSchedule>
    {
        public WrapperExecutorFactory<ICoolingScheduleExecutor> CreateWrapperFactory(ICoolingSchedule schedule) =>
            inner => new Executor(inner, record);

        private sealed class Executor(ICoolingScheduleExecutor schedule, Action<double> record) : ICoolingScheduleExecutor
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
        : IConfigurationNode<AnnealingPlan.Executor>
    {
        public ExecutorFactory<Executor> CreateExecutorFactory() =>
            scope => new Executor(scope.Resolve(Warmup, static source => source.CreateExecutorFactory(), PrepareCoolingAdvice), scope.Resolve(Main, static source => source.CreateExecutorFactory(), PrepareCoolingAdvice), WarmupSteps);

        public sealed class Executor(ICoolingScheduleExecutor warmup, ICoolingScheduleExecutor main, int warmupSteps) : IExecutor
        {
            public double Temperature(int step) =>
                step < warmupSteps ? warmup.Temperature(step) : main.Temperature(step - warmupSteps);
        }
    }

}
