using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public class NodeSelectionSpecs
{
    [Fact]
    public void AReferenceRetainsItsDeclaredRole()
    {
        IMutator<RealVector> mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        NodeSelector<IMutator<RealVector>> selected = NodeSelector.Reference(mutator);

        selected.Matches(mutator).ShouldBeTrue();
        selected.Matches(new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5)).ShouldBeFalse();
    }

    [Fact]
    public void RoleAndConcreteTypeSelectionsSupportConfigurationFilters()
    {
        var smallMutation = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var largeMutation = smallMutation with { MutationStrength = 2.0 };

        NodeSelector<IMutator<RealVector>> selectedMutators = NodeSelector.OfType<IMutator<RealVector>>()
            .And(mutator => mutator is GaussianMutator { MutationStrength: < 1.0 });
        NodeSelector<GaussianMutator> selectedGaussianMutators = NodeSelector.OfType<GaussianMutator>()
            .And(mutator => mutator.MutationStrength < 1.0);

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
        var allMutators = NodeSelector.OfType<IMutator<RealVector>>();
        NodeSelector<IMutator<RealVector>> selected = allMutators
            .And(mutator => mutator is NamedMutator<RealVector> { Name: "exploration" });

        // Matches examines supplied nodes; ResolutionScopeBuilder.Wrap applies a selection while resolving.
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
        var exactReference = NodeSelector.Reference(preferred);
        var smallMutations = NodeSelector.OfType<IMutator<RealVector>>()
            .And(mutator => mutator is GaussianMutator { MutationStrength: < 1.0 });

        NodeSelector<IMutator<RealVector>> selected = exactReference.Or(smallMutations);
        NodeSelector<IMutator<RealVector>> sameSelection = exactReference | smallMutations;
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
        var namedExploration = new NodeSelector<IMutator<RealVector>>(
            mutator => mutator is NamedMutator<RealVector> { Name: "exploration" });

        var namedReferences = NodeSelector.Reference(exploration) | NodeSelector.Reference(refinement);
        NodeSelector<IMutator<RealVector>> selected = namedReferences & namedExploration;
        var sameSelection = namedReferences.And(
            mutator => mutator is NamedMutator<RealVector> { Name: "exploration" });
        IConfigurationNode[] nodes = [child, exploration, refinement];

        nodes.Where(selected.Matches).ShouldBe([exploration]);
        nodes.Where(sameSelection.Matches).ShouldBe([exploration]);
    }

    [Fact]
    public async Task OneModuleCanCountEveryConfiguredMutatorInAnAlgorithm()
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

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .Attach(new MutationCounter(mutations));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        mutations.CurrentCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ConsumerDefinedRolesCanBeSelectedByType()
    {
        ICoolingSchedule warmup = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        ICoolingSchedule main = new GeometricCooling(StartTemperature: 8, Factor: 0.5);
        var plan = new AnnealingPlan(warmup, main, WarmupSteps: 3);
        var queriedTemperatures = new List<double>();

        var scope = ResolutionScope.Create(builder => builder.Wrap(
            NodeSelector.OfType<ICoolingSchedule>(),
            schedule => new RecordedCooling(schedule, queriedTemperatures.Add)));
        var execution = scope.Resolve(plan);

        execution.Temperature(step: 0).ShouldBe(100);
        execution.Temperature(step: 5).ShouldBe(2);
        queriedTemperatures.ShouldBe([100, 2]);
    }

    private sealed class MutationCounter(CountAccumulator mutations) : IExecutionModule
    {
        private static readonly NodeSelector<IMutator<RealVector>> Mutators = NodeSelector.OfType<IMutator<RealVector>>();

        public void Install(ResolutionScopeBuilder builder) =>
            builder.Wrap(Mutators, mutator => mutator.CountCalls(mutations));
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

    private sealed record RecordedCooling(ICoolingSchedule Schedule, Action<double> Record) : ICoolingSchedule
    {
        public ExecutionFactory<ICoolingScheduleExecution> CreateExecutionFactory() =>
            scope => new Execution(scope.Resolve(Schedule), Record);

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
            scope => new Execution(scope.Resolve(Warmup), scope.Resolve(Main), WarmupSteps);

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
