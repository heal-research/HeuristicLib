using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Algorithms;

/// <summary>
/// A problem knows its own encoding, so configuring a genetic algorithm for it needs no operators and no type
/// arguments. The problem is asked first for each role; whatever it declines comes from the encoding.
/// </summary>
public class EncodingAndProblemDefaultSpecs
{
    [Fact]
    public void ProblemWithOwnPreferences_TakesThemAndFallsBackToTheEncodingForTheRest()
    {
        var problem = new TravelingSalesmanProblem();

        GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> algorithm =
            GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        algorithm.Crossover.ShouldBeOfType<OrderCrossover>();
        algorithm.Creator.ShouldBeOfType<RandomPermutationCreator>();
        algorithm.Mutator.ShouldBeOfType<InversionMutator>();
    }

    [Fact]
    public void ProblemWithoutPreferences_TakesEveryOperatorFromTheEncoding()
    {
        var problem = new QuadraticAssignmentProblem(
            new QuadraticAssignmentProblemData(SymmetricMatrix(), SymmetricMatrix()));

        var algorithm = GeneticAlgorithm.For(problem.SearchSpace, populationSize: 20, maximumGenerations: 5);

        algorithm.Creator.ShouldBeOfType<RandomPermutationCreator>();
        algorithm.Crossover.ShouldBeOfType<EdgeRecombinationCrossover>();
        algorithm.Mutator.ShouldBeOfType<InversionMutator>();
    }

    [Fact]
    public async Task DefaultedAlgorithm_RunsWithoutFurtherConfiguration()
    {
        var problem = new TravelingSalesmanProblem();

        var run = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5)
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .TrackBestMedianWorst(out var qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(qualityAnalyzer).Count.ShouldBe(5);
    }

    /// <summary>
    /// A supplied operator wins over both the problem's preference and the encoding's, per role. Everything not
    /// supplied still comes from the defaults.
    /// </summary>
    [Fact]
    public async Task ASuppliedOperator_TakesPrecedenceOverBothDefaultSources()
    {
        var problem = new TravelingSalesmanProblem();

        var algorithm = GeneticAlgorithm.For(
            problem,
            crossover: new EdgeRecombinationCrossover(),
            populationSize: 20,
            maximumGenerations: 5);

        algorithm.Crossover.ShouldBeOfType<EdgeRecombinationCrossover>();
        algorithm.Creator.ShouldBeOfType<RandomPermutationCreator>();
        algorithm.Mutator.ShouldBeOfType<InversionMutator>();

        var finalState = await algorithm
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        finalState.Population.EvaluatedCandidates.Count.ShouldBe(20);
    }

    [Fact]
    public void ASearchSpaceAlone_ConfiguresAnAlgorithmThatRunsAgainstAnyProblemOverIt()
    {
        var searchSpace = new PermutationSearchSpace(5);

        var algorithm = GeneticAlgorithm.For(searchSpace, populationSize: 20, maximumGenerations: 5);

        algorithm.Creator.ShouldBeOfType<RandomPermutationCreator>();
        algorithm.Crossover.ShouldBeOfType<EdgeRecombinationCrossover>();
        algorithm.Mutator.ShouldBeOfType<InversionMutator>();

        var tour = new TravelingSalesmanProblem(
            new TravelingSalesmanDistanceMatrixProblemData(SymmetricMatrix()));
        var assignment = new QuadraticAssignmentProblem(
            new QuadraticAssignmentProblemData(SymmetricMatrix(), SymmetricMatrix()));

        algorithm.Complete(tour, RandomNumberGenerator.Create(seed: 1)).ShouldNotBeNull();
        algorithm.Complete(assignment, RandomNumberGenerator.Create(seed: 1)).ShouldNotBeNull();
    }

    [Fact]
    public void RemainingAlgorithms_InferTheirTypesFromAProblemAndTakeItsRequiredDefaults()
    {
        var problem = new TravelingSalesmanProblem();

        EvolutionStrategy<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> evolutionStrategy =
            EvolutionStrategy.For(problem);
        NSGA2<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> nsga2 = NSGA2.For(problem);
        HillClimber<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> hillClimber =
            HillClimber.For(problem);

        evolutionStrategy.Creator.ShouldBeOfType<RandomPermutationCreator>();
        evolutionStrategy.Mutator.ShouldBeOfType<InversionMutator>();
        evolutionStrategy.Crossover.ShouldBeNull();

        nsga2.Creator.ShouldBeOfType<RandomPermutationCreator>();
        nsga2.Crossover.ShouldBeOfType<OrderCrossover>();
        nsga2.Mutator.ShouldBeOfType<InversionMutator>();

        hillClimber.Creator.ShouldBeOfType<RandomPermutationCreator>();
        hillClimber.Mutator.ShouldBeOfType<InversionMutator>();
    }

    [Fact]
    public void RemainingAlgorithms_InferTheirTypesFromASearchSpaceAndTakeItsRequiredDefaults()
    {
        var searchSpace = new PermutationSearchSpace(5);

        EvolutionStrategy<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> evolutionStrategy =
            EvolutionStrategy.For(searchSpace);
        NSGA2<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> nsga2 =
            NSGA2.For(searchSpace);
        HillClimber<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> hillClimber =
            HillClimber.For(searchSpace);

        evolutionStrategy.Creator.ShouldBeOfType<RandomPermutationCreator>();
        evolutionStrategy.Mutator.ShouldBeOfType<InversionMutator>();
        evolutionStrategy.Crossover.ShouldBeNull();

        nsga2.Creator.ShouldBeOfType<RandomPermutationCreator>();
        nsga2.Crossover.ShouldBeOfType<EdgeRecombinationCrossover>();
        nsga2.Mutator.ShouldBeOfType<InversionMutator>();

        hillClimber.Creator.ShouldBeOfType<RandomPermutationCreator>();
        hillClimber.Mutator.ShouldBeOfType<InversionMutator>();
    }

    [Fact]
    public void RemainingCreateFactories_InferEveryTypeFromTheirRequiredOperators()
    {
        EvolutionStrategy<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> evolutionStrategy =
            EvolutionStrategy.Create(new RandomPermutationCreator(), new InversionMutator());
        NSGA2<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> nsga2 =
            NSGA2.Create(new RandomPermutationCreator(), new EdgeRecombinationCrossover(), new InversionMutator());
        HillClimber<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> hillClimber =
            HillClimber.Create(new RandomPermutationCreator(), new InversionMutator());

        evolutionStrategy.ShouldNotBeNull();
        nsga2.ShouldNotBeNull();
        hillClimber.ShouldNotBeNull();
    }

    private static double[,] SymmetricMatrix() => new double[,]
    {
        { 0, 1, 2, 3, 4 },
        { 1, 0, 1, 2, 3 },
        { 2, 1, 0, 1, 2 },
        { 3, 2, 1, 0, 1 },
        { 4, 3, 2, 1, 0 }
    };
}
