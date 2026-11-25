using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentMaterializationTests
{
    [Fact]
    public void GridExposesItsPrototypeAndHasNoDimensionsInitially()
    {
        var prototype = ExperimentTestSupport.CreateGeneticAlgorithm();
        var grid = prototype.AsParameterGrid();

        grid.Prototype.ShouldBeSameAs(prototype);
        grid.Dimensions.ShouldBeEmpty();
    }

    [Fact]
    public void GridExposesDescriptorNameAndLambdaDimensionsInDeclarationOrder()
    {
        var prototype = ExperimentTestSupport.CreateGeneticAlgorithm();
        var size = GeneticAlgorithm<RealVector>.PopulationSizeParameter;
        var rate = GeneticAlgorithm<RealVector>.MutationRateParameter;
        var grid = prototype.AsParameterGrid()
            .VaryBy(size, [40, 20])
            .VaryBy(rate.Name, [0.3, 0.1])
            .VaryBy([1, 2], (algorithm, elites) => algorithm with { Elites = elites });

        var dimensions = grid.Dimensions;

        dimensions.Count.ShouldBe(3);
        dimensions[0].Parameter.ShouldBeSameAs(size);
        dimensions[0].Values.ShouldBe([40, 20]);
        dimensions[1].Parameter.ShouldBeSameAs(rate);
        dimensions[1].Values.ShouldBe([0.3, 0.1]);
        dimensions[2].Parameter.ShouldBeNull();
        dimensions[2].Values.ShouldBe([1, 2]);
    }

    [Fact]
    public void GridExposesRepeatedVariationOfAParameterAsSeparateDimensions()
    {
        var prototype = ExperimentTestSupport.CreateGeneticAlgorithm();
        var size = GeneticAlgorithm<RealVector>.PopulationSizeParameter;
        var grid = prototype.AsParameterGrid().VaryBy(size, [20, 40]).VaryBy(size.Name, [60]);

        grid.Dimensions.ShouldBe([(size, [20, 40]), (size, [60])]);
    }

    [Fact]
    public void ExperimentExposesItsGridsInSuppliedOrder()
    {
        var first = new AdditiveStepAlgorithm(10).AsParameterGrid();
        var second = new AdditiveStepAlgorithm(20).AsParameterGrid();
        var experiment = Experiment.Create([second, first]);

        experiment.Grids.ShouldBe([second, first]);
    }

    [Fact]
    public void ParameterCandidatesDecodeInCartesianOrderAcrossGrids()
    {
        var prototype = new AdditiveStepAlgorithm(0);
        var first = prototype.AsParameterGrid()
            .VaryBy([10, 20], (algorithm, tens) => algorithm with { Increment = tens })
            .VaryBy([1, 2], (algorithm, ones) => algorithm with { Increment = algorithm.Increment + ones });
        var last = prototype.AsParameterGrid()
            .VaryBy([70, 80], (algorithm, value) => algorithm with { Increment = value })
            .VaryBy([1, 2], (algorithm, ones) => algorithm with { Increment = algorithm.Increment + ones });
        var experiment = Experiment.Create([first, new AdditiveStepAlgorithm(50).AsParameterGrid(), new AdditiveStepAlgorithm(60).AsParameterGrid(), last]);

        var trials = ExperimentTestSupport.CreateRun(experiment).Trials;

        trials.Select(trial => (trial.ParameterCandidateIndex, trial.Algorithm.Increment, trial.Repetition)).ShouldBe([
            (0, 11, 0), (1, 12, 0), (2, 21, 0), (3, 22, 0), (4, 50, 0),
            (5, 60, 0), (6, 71, 0), (7, 72, 0), (8, 81, 0), (9, 82, 0)
        ]);
        Enumerable.Range(0, trials.Length).Select(index => experiment.Decode(index).Algorithm.Increment)
            .ShouldBe(trials.Select(trial => trial.Algorithm.Increment));
    }

    [Fact]
    public void DescriptorNameAndLambdaProduceEqualConfigurations()
    {
        var algorithm = ExperimentTestSupport.CreateGeneticAlgorithm();
        var problem = new TestFunctionProblem(new RastriginFunction(2));
        var byDescriptor = algorithm.AsParameterGrid().VaryBy(GeneticAlgorithm<RealVector>.PopulationSizeParameter, [20, 40]);
        var byName = algorithm.AsParameterGrid().VaryBy("PopulationSize", [20, 40]);
        var byLambda = algorithm.AsParameterGrid().VaryBy([20, 40], (configuration, size) => configuration with { PopulationSize = size });

        var descriptorRun = Experiment.Create(byDescriptor).CreateRun(problem, RandomNumberGenerator.Create(42));
        var nameRun = Experiment.Create(byName).CreateRun(problem, RandomNumberGenerator.Create(42));
        var lambdaRun = Experiment.Create(byLambda).CreateRun(problem, RandomNumberGenerator.Create(42));

        descriptorRun.Trials.Select(trial => trial.Algorithm).ShouldBe(lambdaRun.Trials.Select(trial => trial.Algorithm));
        nameRun.Trials.Select(trial => trial.Algorithm).ShouldBe(descriptorRun.Trials.Select(trial => trial.Algorithm));
        var parameter = GeneticAlgorithm<RealVector>.PopulationSizeParameter;
        nameRun.Trials.SelectMany(trial => trial.Settings).ShouldBe([(parameter, 20), (parameter, 40)]);
        descriptorRun.Trials.SelectMany(trial => trial.Settings).ShouldBe([(parameter, 20), (parameter, 40)]);
        lambdaRun.Trials.ShouldAllBe(trial => trial.Settings.IsEmpty);
        byDescriptor.ShouldBe(algorithm.AsParameterGrid().VaryBy(GeneticAlgorithm<RealVector>.PopulationSizeParameter, [20, 40]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InheritedParameterPreservesTheConcreteAlgorithmType(bool byName)
    {
        var algorithm = ExperimentTestSupport.CreateGeneticAlgorithm() with { PopulationSize = 20 };
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var parameter = GeneticAlgorithm<RealVector>.InterceptorParameter;
        var grid = byName ? algorithm.AsParameterGrid().VaryBy(parameter.Name, [interceptor]) : algorithm.AsParameterGrid().VaryBy(parameter, [interceptor]);
        var run = Experiment.Create(grid).CreateRun(new TestFunctionProblem(new RastriginFunction(2)), RandomNumberGenerator.Create(42));

        var trial = run.Trials.Single();
        trial.Algorithm.PopulationSize.ShouldBe(20);
        trial.Algorithm.Interceptor.ShouldBeSameAs(interceptor);
        trial.Settings.Single().ShouldBe((parameter, interceptor));
    }

    [Fact]
    public void LabelsReadFinalSettingsInFirstDeclarationOrderAndBelongToTheirGrid()
    {
        var algorithm = ExperimentTestSupport.CreateGeneticAlgorithm() with { PopulationSize = 20 };
        var size = GeneticAlgorithm<RealVector>.PopulationSizeParameter;
        var rate = GeneticAlgorithm<RealVector>.MutationRateParameter;
        var grid = algorithm.AsParameterGrid()
            .VaryBy(size, [10])
            .VaryBy(rate, [0.2])
            .VaryBy(size.Name, [20])
            .VaryBy([5], (configuration, extra) => configuration with { PopulationSize = configuration.PopulationSize + extra });
        var experiment = Experiment.Create([grid, algorithm.AsParameterGrid()]);

        var run = experiment.CreateRun(new TestFunctionProblem(new RastriginFunction(2)), RandomNumberGenerator.Create(42));

        run.Trials[0].Settings.AsEnumerable().ShouldBe([(size, 25), (rate, 0.2)]);
        run.Trials[1].Settings.ShouldBeEmpty();
    }

    [Fact]
    public void CycleAlgorithmListsCanBeVariedByDescriptorAndName()
    {
        var ga = ExperimentTestSupport.CreateGeneticAlgorithm();
        var es = new EvolutionStrategy<RealVector> { Creator = ga.Creator, Mutator = ga.Mutator };
        var cycle = CycleAlgorithm.Create(ga, es);
        var parameter = CycleAlgorithm<RealVector, PopulationState<RealVector>>.AlgorithmsParameter;
        var byDescriptor = cycle.AsParameterGrid().VaryBy(CycleAlgorithm<RealVector, PopulationState<RealVector>>.AlgorithmsParameter, [[ga, es], [ga]]);
        var byName = cycle.AsParameterGrid().VaryBy<ValueArray<IAlgorithm<RealVector, PopulationState<RealVector>>>>("Algorithms", [[ga, es], [ga]]);
        var problem = new TestFunctionProblem(new RastriginFunction(2));

        var run = Experiment.Create([byDescriptor, byName]).CreateRun(problem, RandomNumberGenerator.Create(42));

        run.Trials.Select(trial => trial.Algorithm.Algorithms.Count).ShouldBe([2, 1, 2, 1]);
        foreach (var trial in run.Trials)
        {
            var (descriptor, value) = trial.Settings.Single();
            descriptor.ShouldBeSameAs(parameter);
            value.ShouldBeOfType<ValueArray<IAlgorithm<RealVector, PopulationState<RealVector>>>>().ShouldBe(trial.Algorithm.Algorithms);
        }
    }

    [Fact]
    public void NameVariationAcceptsUnderlyingValuesAndNullForNullableParameters()
    {
        var cycle = CycleAlgorithm.Create(new ProbeAlgorithm(0));
        var integers = cycle.AsParameterGrid().VaryBy("MaximumCycles", [2]);
        var includesNull = cycle.AsParameterGrid().VaryBy<int?>("MaximumCycles", [null, 3]);

        var run = ExperimentTestSupport.CreateRun(Experiment.Create([integers, includesNull]));

        run.Trials.Select(trial => trial.Algorithm.MaximumCycles).ShouldBe([2, null, 3]);
    }

    [Fact]
    public void NameVariationRejectsUnknownAmbiguousAndIncompatibleParametersImmediately()
    {
        var ga = ExperimentTestSupport.CreateGeneticAlgorithm();
        var grid = ga.AsParameterGrid();
        var ambiguous = new DuplicateParameterAlgorithm { Creator = ga.Creator, Crossover = ga.Crossover, Mutator = ga.Mutator };
        Should.Throw<ArgumentException>(() => grid.VaryBy("Selector.TournamentSize", [2])).Message.ShouldContain("Selector.TournamentSize");
        Should.Throw<ArgumentException>(() => grid.VaryBy("PopulationSize", [0.1])).Message.ShouldContain("PopulationSize");
        Should.Throw<ArgumentException>(() => ambiguous.AsParameterGrid().VaryBy("PopulationSize", [2])).Message.ShouldContain("PopulationSize");
    }

    private sealed record DuplicateParameterAlgorithm : GeneticAlgorithm<RealVector>
    {
        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), PopulationSizeParameter];
    }

    [Fact]
    public void EqualConfigurationsHaveDistinctParameterCandidateIndexesAndRuns()
    {
        var grid = new AdditiveStepAlgorithm(0).AsParameterGrid()
            .VaryBy([1, 1], (algorithm, increment) => algorithm with { Increment = increment });

        var trials = ExperimentTestSupport.CreateRun(Experiment.Create(grid)).Trials;

        trials.Select(trial => (trial.ParameterCandidateIndex, trial.Repetition)).ShouldBe([(0, 0), (1, 0)]);
        trials[0].Algorithm.ShouldBe(trials[1].Algorithm);
        trials[0].Run.ShouldNotBeSameAs(trials[1].Run);
    }

    [Fact]
    public void ExperimentSnapshotsInputsAndDecodesAtRunCreation()
    {
        var increments = new List<int> { 1, 2 };
        var calls = 0;
        var grid = new AdditiveStepAlgorithm(0).AsParameterGrid()
            .VaryBy(increments, (algorithm, increment) =>
            {
                calls++;
                return algorithm with { Increment = increment };
            });
        var grids = new[] { grid, new AdditiveStepAlgorithm(3).AsParameterGrid() };
        var experiment = Experiment.Create(grids, repetitions: 2);

        increments[0] = 10;
        grids[1] = new AdditiveStepAlgorithm(30).AsParameterGrid();
        calls.ShouldBe(0);

        var trials = ExperimentTestSupport.CreateRun(experiment).Trials;
        calls.ShouldBe(2);
        trials.Select(trial => (trial.Algorithm.Increment, trial.Repetition))
            .ShouldBe([(1, 0), (1, 1), (2, 0), (2, 1), (3, 0), (3, 1)]);
    }

    [Fact]
    public void EmptyDimensionsGridListsAndProblemListsAreRejectedImmediately()
    {
        var grid = ExperimentTestSupport.CreateGeneticAlgorithm().AsParameterGrid();
        Should.Throw<ArgumentException>(() => grid.VaryBy<int>([], (algorithm, size) => algorithm with { PopulationSize = size }))
            .Message.ShouldContain("lambda dimension");
        Should.Throw<ArgumentException>(() => grid.VaryBy(GeneticAlgorithm<RealVector>.PopulationSizeParameter, []))
            .Message.ShouldContain("PopulationSize");
        Should.Throw<ArgumentException>(() => grid.VaryBy<int>("PopulationSize", [])).Message.ShouldContain("PopulationSize");
        Should.Throw<ArgumentException>(() => Experiment.Create<int, ProbeAlgorithm, PopulationState<int>>([])).ParamName.ShouldBe("grids");
        var failure = Should.Throw<ArgumentException>(() => Experiment.Create(grid).CreateRun(Array.Empty<TestFunctionProblem>(), RandomNumberGenerator.Create(42)));
        failure.ParamName.ShouldBe("problems");
        failure.Message.ShouldStartWith("Supply at least one problem.");
    }

    [Fact]
    public void UntypedWriteRejectsAnIncompatibleValue()
    {
        var algorithm = ExperimentTestSupport.CreateGeneticAlgorithm();
        IParameter parameter = GeneticAlgorithm<RealVector>.PopulationSizeParameter;

        var failure = Should.Throw<ArgumentException>(() => parameter.WithValue(algorithm, 20L));

        failure.ParamName.ShouldBe("value");
        failure.Message.ShouldContain("PopulationSize");
    }

    [Fact]
    public void DecodingFailureReportsGlobalParameterCandidateIndexAndOriginalException()
    {
        var prototype = new ProbeAlgorithm(0);
        var failure = new InvalidOperationException("Broken configurator");
        var grid = prototype.AsParameterGrid().VaryBy([10, 20], (algorithm, value) => value == 20 ? throw failure : algorithm with { Value = value });
        var experiment = Experiment.Create([prototype.AsParameterGrid(), grid]);

        var exception = Should.Throw<InvalidOperationException>(() => ExperimentTestSupport.CreateRun(experiment));
        exception.Message.ShouldContain("parameter candidate 2");
        exception.InnerException.ShouldBeSameAs(failure);
        var indexedException = Should.Throw<InvalidOperationException>(() => experiment.Decode(2));
        indexedException.Message.ShouldBe(exception.Message);
        indexedException.InnerException.ShouldBeSameAs(failure);
    }

    [Fact]
    public void ProblemsAndParameterCandidatesDetermineTheStartingStreams()
    {
        var prototype = new ProbeAlgorithm(0);
        var experiment = Experiment.Create([prototype.AsParameterGrid(), (prototype with { Value = 1 }).AsParameterGrid()], repetitions: 2);
        var first = FuncProblem.Create((int value) => value, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var second = FuncProblem.Create((int value) => -value, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

        var shared = experiment.CreateRun([first, second], RandomNumberGenerator.Create(42));
        var independent = experiment with { ShareRandomStreamsAcrossParameterCandidates = false };
        var independentRun = independent.CreateRun([first, second], RandomNumberGenerator.Create(42));

        shared.Trials.Select(trial => (trial.ProblemPosition, trial.ParameterCandidateIndex, trial.Repetition)).ShouldBe([
            (0, 0, 0), (0, 0, 1), (0, 1, 0), (0, 1, 1), (1, 0, 0), (1, 0, 1), (1, 1, 0), (1, 1, 1)
        ]);
        FuncProblem<int, DummySearchSpace<int>> concreteProblem = shared.Trials[0].Problem;
        concreteProblem.ShouldBeSameAs(first);
        shared.Trials[4].Problem.ShouldBeSameAs(second);
        shared.Trials.Select(trial => trial.Run.Random).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(8);

        var draws = shared.Trials.Select(trial => trial.Run.Random.Fork(0).NextInt()).ToArray();
        draws[..2].ShouldBe(draws[2..4]);
        draws[4..6].ShouldBe(draws[6..8]);
        new[] { draws[0], draws[1], draws[4], draws[5] }.Distinct().Count().ShouldBe(4);
        independentRun.Trials.Select(trial => trial.Run.Random).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(8);
        independentRun.Trials.Select(trial => trial.Run.Random.Fork(0).NextInt()).Distinct().Count().ShouldBe(8);
    }

    [Fact]
    public void InterfaceProblemListKeepsDifferentProblemInstances()
    {
        var first = new TestFunctionProblem(new RastriginFunction(2));
        var second = FuncProblem.Create((RealVector value) => value[0], first.SearchSpace, SingleObjective.Minimize);
        IReadOnlyList<IProblem<RealVector, BoundedRealVectorSearchSpace>> problems = [first, second];

        var grid = ExperimentTestSupport.CreateGeneticAlgorithm().AsParameterGrid();
        var run = Experiment.Create(grid).CreateRun(problems, RandomNumberGenerator.Create(42));

        run.Trials[0].Problem.ShouldBeSameAs(first);
        run.Trials[1].Problem.ShouldBeSameAs(second);
        run.Trials.Select(trial => trial.ProblemPosition).ShouldBe([0, 1]);
    }
}
