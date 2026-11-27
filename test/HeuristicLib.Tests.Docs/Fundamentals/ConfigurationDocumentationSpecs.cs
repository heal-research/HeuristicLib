using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.Fundamentals;

public sealed class ConfigurationDocumentationSpecs
{
    [Fact]
    public void ChildConfigurationsAreIdentifiedByReference()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(2));

        #region configuration-graph
        var mutator = new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2);
        var lookAlike = new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2);
        var algorithm = GeneticAlgorithm.For(problem,
            creator: new UniformDistributedCreator(), crossover: new AlphaBetaBlendCrossover(),
            mutator: mutator, maximumGenerations: 100);

        // Every declared parameter reports the child configurations it holds.
        var children = algorithm.GetParameters()
            .SelectMany(parameter => parameter.GetChildren(algorithm))
            .ToList();

        var holdsMutator = children.Any(child => ReferenceEquals(child, mutator));
        var holdsLookAlike = children.Any(child => ReferenceEquals(child, lookAlike));
        #endregion configuration-graph

        lookAlike.ShouldBe(mutator);
        holdsMutator.ShouldBeTrue();
        holdsLookAlike.ShouldBeFalse();
    }

    [Fact]
    public void ParametersReadAConfigurationAndCreateCopies()
    {
        var algorithm = CreateAlgorithm(new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2));

        #region configuration-parameters
        var populationSize = GeneticAlgorithm<RealVector>.PopulationSizeParameter;
        var larger = populationSize.With(algorithm, populationSize.Get(algorithm) * 2);

        foreach (var parameter in larger.GetParameters())
            Console.WriteLine($"{parameter.Name}: {parameter.GetValue(larger)}");
        #endregion configuration-parameters

        populationSize.Get(algorithm).ShouldBe(20);
        populationSize.Get(larger).ShouldBe(40);
        larger.GetParameters().Select(parameter => parameter.Name).ShouldContain(nameof(GeneticAlgorithm<RealVector>.PopulationSize));
    }

    [Fact]
    public void SubstitutingByReferenceReturnsTheConfigurationThatRuns()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(2));
        var mutator = new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2);
        var lookAlike = new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2);
        var algorithm = CreateAlgorithm(mutator);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region configuration-substitute-by-reference
        var finer = algorithm.Substitute(mutator, gaussian => gaussian with { MutationStrength = 0.05 });
        var finalState = finer.RunToCompletion(problem, RandomNumberGenerator.Create(seed: 42), ct: cancellationToken);
        #endregion configuration-substitute-by-reference

        ((GaussianMutator)finer.Mutator).MutationStrength.ShouldBe(0.05);
        algorithm.Mutator.ShouldBeSameAs(mutator);
        finer.ShouldNotBeSameAs(algorithm);
        finalState.Population.EvaluatedCandidates.Count.ShouldBe(20);
        Should.Throw<InvalidOperationException>(() => algorithm.Substitute(lookAlike, gaussian => gaussian with { MutationStrength = 0.05 }));
    }

    [Fact]
    public void SubstitutingByTypeOrPredicateReachesEverySelectedConfiguration()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(2));

        #region configuration-substitute-by-type
        // A cycle of two algorithms that differ in the strength of their Gaussian mutator.
        var exploration = GeneticAlgorithm.For(problem,
            creator: new UniformDistributedCreator(), crossover: new AlphaBetaBlendCrossover(),
            mutator: new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.5), maximumGenerations: 100);
        var exploitation = exploration with { Mutator = new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.01) };
        var cycle = CycleAlgorithm.Create(exploration, exploitation) with { MaximumCycles = 2 };

        // Every GaussianMutator in the graph.
        var narrower = cycle.Substitute((GaussianMutator gaussian) => gaussian with { MutationStrength = 0.05 });

        // Only the Gaussian mutators the predicate accepts.
        var narrowedWide = cycle.Substitute(
            (GaussianMutator gaussian) => gaussian.MutationStrength > 0.1,
            gaussian => gaussian with { MutationStrength = 0.05 });

        // A selection that matches nothing changes nothing.
        var unchanged = cycle.Substitute(
            (GaussianMutator gaussian) => gaussian.MutationStrength > 10,
            gaussian => gaussian with { MutationStrength = 0.05 });
        #endregion configuration-substitute-by-type

        StrengthsOf(narrower).ShouldBe([0.05, 0.05]);
        StrengthsOf(narrowedWide).ShouldBe([0.05, 0.01]);
        StrengthsOf(cycle).ShouldBe([0.5, 0.01]);
        unchanged.ShouldBeSameAs(cycle);
    }

    [Fact]
    public async Task SubstitutionsOfCreateRunApplyToThatRunOnly()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(2));
        var algorithm = CreateAlgorithm(new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2), maximumGenerations: 3);
        var evaluator = algorithm.Evaluator;
        var cancellationToken = TestContext.Current.CancellationToken;

        #region configuration-substitute-for-one-run
        var evaluations = new Counter();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42),
            [algorithm.Evaluator.Invocations.Counting(evaluations)]);
        await run.CompleteAsync(cancellationToken: cancellationToken);

        var evaluationCalls = run.Result(evaluations).CurrentCount;
        #endregion configuration-substitute-for-one-run

        evaluationCalls.ShouldBeGreaterThan(0);
        run.Algorithm.ShouldNotBeSameAs(algorithm);
        ((GeneticAlgorithm<RealVector>)run.Algorithm).Evaluator.ShouldNotBeSameAs(evaluator);
        algorithm.Evaluator.ShouldBeSameAs(evaluator);
    }

    [Fact]
    public async Task SharingKeepsOneExecutorAcrossTheTurnsOfACycle()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(2));
        var cancellationToken = TestContext.Current.CancellationToken;

        #region configuration-sharing
        var terminator = new AfterIterationsTerminator<RealVector>(maximumIterations: 3);
        var inner = GeneticAlgorithm.For(problem,
            creator: new UniformDistributedCreator(), crossover: new AlphaBetaBlendCrossover(),
            mutator: new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.2),
            terminator: terminator, maximumGenerations: null);
        var cycle = CycleAlgorithm.Create(inner) with { MaximumCycles = 3 };

        var separate = new Counter();
        var separateRun = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [inner.Iterations.Counting(separate)]);
        await separateRun.CompleteAsync(cancellationToken: cancellationToken);

        var kept = new Counter();
        var keptRun = cycle.Sharing(terminator).CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [inner.Iterations.Counting(kept)]);
        await keptRun.CompleteAsync(cancellationToken: cancellationToken);

        Console.WriteLine($"iterations without Sharing: {separateRun.Result(separate).CurrentCount}");
        Console.WriteLine($"iterations with Sharing:    {keptRun.Result(kept).CurrentCount}");
        #endregion configuration-sharing

        separateRun.Result(separate).CurrentCount.ShouldBe(9);
        keptRun.Result(kept).CurrentCount.ShouldBe(5);
    }

    [Fact]
    public async Task ChangingAConfigurationUsesWithSubstituteOrCreateRun()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = GeneticAlgorithm.Create(
            new UniformDistributedCreator(),
            new AlphaBetaBlendCrossover { Alpha = 0.7 },
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            selector: TournamentSelector.For(problem, tournamentSize: 2),
            populationSize: 200,
            maximumGenerations: 500,
            mutationRate: 0.2);

#pragma warning disable xUnit1051 // The displayed program has no cancellation token.
        #region configuration-change-workflow
        // A setting of the algorithm itself: `with` creates a changed copy.
        var largerPopulation = algorithm with { PopulationSize = 400 };

        // Every GaussianMutator in the graph, wherever it sits: Substitute.
        var finerMutation = algorithm.Substitute(
            (GaussianMutator gaussian) => gaussian with { MutationStrength = 0.05 });

        // Instrumentation for one run only: the substitutions list of CreateRun.
        var evaluations = new Counter();
        var run = finerMutation.CreateRun(
            problem,
            RandomNumberGenerator.Create(seed: 123),
            [finerMutation.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await run.CompleteAsync();

        Console.WriteLine($"Evaluated candidates: {run.Result(evaluations).CurrentCount}");
        #endregion configuration-change-workflow
#pragma warning restore xUnit1051

        largerPopulation.PopulationSize.ShouldBe(400);
        algorithm.PopulationSize.ShouldBe(200);
        ((GaussianMutator)finerMutation.Mutator).MutationStrength.ShouldBe(0.05);
        ((GaussianMutator)algorithm.Mutator).MutationStrength.ShouldBe(0.15);
        finerMutation.Evaluator.ShouldBeSameAs(algorithm.Evaluator);
        run.Algorithm.ShouldNotBeSameAs(finerMutation);
        run.Result(evaluations).CurrentCount.ShouldBeGreaterThan(0);
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(GaussianMutator mutator, int? maximumGenerations = 100)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(2));
        return GeneticAlgorithm.For(problem,
            creator: new UniformDistributedCreator(), crossover: new AlphaBetaBlendCrossover(),
            mutator: mutator, populationSize: 20, maximumGenerations: maximumGenerations);
    }

    private static double[] StrengthsOf(CycleAlgorithm<RealVector, PopulationState<RealVector>> cycle) =>
        [.. cycle.Algorithms.Cast<GeneticAlgorithm<RealVector>>().Select(algorithm => ((GaussianMutator)algorithm.Mutator).MutationStrength)];
}
