using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Algorithms;

/// <summary>
/// A problem knows its own search space, so configuring a genetic algorithm for it needs no operators and no type
/// arguments when recommendations cover every required role. The problem is asked first for each role, then its
/// search space.
/// </summary>
public class OperatorRecommendationSpecs
{
    [Fact]
    public void ProblemWithOwnRecommendations_TakesThemAndFallsBackToTheSearchSpaceForTheRest()
    {
        var problem = new TravelingSalesmanProblem();

        GeneticAlgorithm<Permutation> algorithm =
            GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        algorithm.Crossover.ShouldBeOfType<OrderCrossover>();
        algorithm.Creator.ShouldBeOfType<RandomPermutationCreator>();
        algorithm.Mutator.ShouldBeOfType<InversionMutator>();
    }

    [Fact]
    public void ProblemWithoutRecommendations_TakesEveryOperatorFromTheSearchSpace()
    {
        var problem = new QuadraticAssignmentProblem(
            new QuadraticAssignmentProblemData(SymmetricMatrix(), SymmetricMatrix()));

        var algorithm = GeneticAlgorithm.For(problem.SearchSpace, populationSize: 20, maximumGenerations: 5);

        algorithm.Creator.ShouldBeOfType<RandomPermutationCreator>();
        algorithm.Crossover.ShouldBeOfType<EdgeRecombinationCrossover>();
        algorithm.Mutator.ShouldBeOfType<InversionMutator>();
    }

    [Fact]
    public async Task RecommendedAlgorithm_RunsWithoutFurtherConfiguration()
    {
        var problem = new TravelingSalesmanProblem();

        var run = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5)
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .TrackBestMedianWorst(out var qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(qualityAnalyzer).Count.ShouldBe(5);
    }

    /// <summary>
    /// A supplied operator wins over both recommendation sources for its role. Recommendations still fill every
    /// parameter the caller omits.
    /// </summary>
    [Fact]
    public async Task ASuppliedOperator_TakesPrecedenceOverBothRecommendationSources()
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

        algorithm.Complete(tour, RandomNumberGenerator.Create(seed: 1), ct: TestContext.Current.CancellationToken).ShouldNotBeNull();
        algorithm.Complete(assignment, RandomNumberGenerator.Create(seed: 1), ct: TestContext.Current.CancellationToken).ShouldNotBeNull();
    }

    [Fact]
    public void EachFactoryCallCreatesFreshRecommendedOperators()
    {
        var problem = new TravelingSalesmanProblem();

        var first = GeneticAlgorithm.For(problem);
        var second = GeneticAlgorithm.For(problem);

        first.Creator.ShouldNotBeSameAs(second.Creator);
        first.Crossover.ShouldNotBeSameAs(second.Crossover);
        first.Mutator.ShouldNotBeSameAs(second.Mutator);
    }

    [Fact]
    public void AConsumerProblemCanRecommendOperatorsWithoutChangingItsSearchSpace()
    {
        var problem = new RecommendedRealVectorProblem(dimension: 3);

        GeneticAlgorithm<RealVector> algorithm = GeneticAlgorithm.For(problem);

        var creator = algorithm.Creator.ShouldBeOfType<UniformDistributedCreator>();
        creator.Minimum.ShouldBe(problem.SearchSpace.Minimum);
        creator.Maximum.ShouldBe(problem.SearchSpace.Maximum);
        algorithm.Crossover.ShouldBeOfType<SinglePointCrossover>();
        algorithm.Mutator.ShouldBeOfType<GaussianMutator>();
    }

    [Fact]
    public void ConsumerDefinedRolesParticipateThroughTheGenericRecommendationContract()
    {
        var source = new ConsumerRecommendationSource();
        var resolution = new OperatorRecommendationResolution(problem: null, source);

        var recommendation = resolution.GetOrRecommend<IConsumerOperator>("consumerOperator", supplied: null);
        resolution.ThrowIfIncomplete("ConsumerAlgorithm");

        recommendation.ShouldBeOfType<ConsumerOperator>();
    }

    [Fact]
    public void ADeclinedProblemRecommendation_FallsBackToTheSearchSpace()
    {
        var problem = new DecliningConsumerRecommendationProblem();
        var searchSpace = new ConsumerRecommendationSource();
        var resolution = new OperatorRecommendationResolution(problem, searchSpace);

        var recommendation = resolution.GetOrRecommend<IConsumerOperator>("consumerOperator", supplied: null);
        resolution.ThrowIfIncomplete("ConsumerAlgorithm");

        recommendation.ShouldBeOfType<ConsumerOperator>();
    }

    [Fact]
    public void BuiltInRoleInterfacesAdaptToTheGenericRecommendationContract()
    {
        var searchSpace = new PermutationSearchSpace(5);
        var resolution = new OperatorRecommendationResolution(problem: null, searchSpace);

        var recommendation = resolution.GetOrRecommend<ICreator<Permutation>>("creator", supplied: null);
        resolution.ThrowIfIncomplete("ConsumerAlgorithm");

        recommendation.ShouldBeOfType<RandomPermutationCreator>();
    }

    [Fact]
    public void RemainingAlgorithms_InferTheirTypesFromAProblemAndTakeItsRequiredRecommendations()
    {
        var problem = new TravelingSalesmanProblem();

        EvolutionStrategy<Permutation> evolutionStrategy =
            EvolutionStrategy.For(problem);
        NSGA2<Permutation> nsga2 = NSGA2.For(problem);
        HillClimber<Permutation> hillClimber =
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
    public void RemainingAlgorithms_InferTheirTypesFromASearchSpaceAndTakeItsRequiredRecommendations()
    {
        var searchSpace = new PermutationSearchSpace(5);

        EvolutionStrategy<Permutation> evolutionStrategy =
            EvolutionStrategy.For(searchSpace);
        NSGA2<Permutation> nsga2 =
            NSGA2.For(searchSpace);
        HillClimber<Permutation> hillClimber =
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

    /// <summary>
    /// The <c>Create</c> factories name only the candidate type, and the operators supply it, so a call site writes no
    /// type arguments at all. The declared result types are the assertion: each is the one argument form.
    /// </summary>
    [Fact]
    public void CreateFactories_InferEverythingTheyNameFromTheirOperators()
    {
        EvolutionStrategy<Permutation> evolutionStrategy =
            EvolutionStrategy.Create(new RandomPermutationCreator(), new InversionMutator());
        NSGA2<Permutation> nsga2 =
            NSGA2.Create(new RandomPermutationCreator(), new EdgeRecombinationCrossover(), new InversionMutator());
        HillClimber<Permutation> hillClimber =
            HillClimber.Create(new RandomPermutationCreator(), new InversionMutator());

        evolutionStrategy.Creator.ShouldBeOfType<RandomPermutationCreator>();
        nsga2.Crossover.ShouldBeOfType<EdgeRecombinationCrossover>();
        hillClimber.Mutator.ShouldBeOfType<InversionMutator>();
    }

    private static double[,] SymmetricMatrix() => new double[,]
    {
        { 0, 1, 2, 3, 4 },
        { 1, 0, 1, 2, 3 },
        { 2, 1, 0, 1, 2 },
        { 3, 2, 1, 0, 1 },
        { 4, 3, 2, 1, 0 }
    };

    private interface IConsumerOperator : IOperator;

    private sealed record ConsumerOperator : IConsumerOperator;

    private sealed class ConsumerRecommendationSource : ISearchSpace, IRecommends<IConsumerOperator>
    {
        public bool TryCreateRecommendedOperator(
            [NotNullWhen(true)] out IConsumerOperator? recommendation)
        {
            recommendation = new ConsumerOperator();
            return true;
        }
    }

    private sealed class DecliningConsumerRecommendationProblem : IProblem, IRecommends<IConsumerOperator>
    {
        public ObjectiveDirections Objective => SingleObjective.Minimize;

        public bool TryCreateRecommendedOperator(
            [NotNullWhen(true)] out IConsumerOperator? recommendation)
        {
            recommendation = null;
            return false;
        }
    }

    private sealed class RecommendedRealVectorProblem(int dimension)
        : RealVectorProblem<RecommendedRealVectorProblem>(
            SingleObjective.Minimize,
            new BoundedRealVectorSearchSpace(dimension, minimum: -1.0, maximum: 1.0)),
          IRecommends<ICreator<RealVector>>,
          IRecommends<ICrossover<RealVector>>,
          IRecommends<IMutator<RealVector>>
    {
        public bool TryCreateRecommendedOperator(
            [NotNullWhen(true)] out ICreator<RealVector>? recommendation)
        {
            recommendation = new UniformDistributedCreator(SearchSpace);
            return true;
        }

        public bool TryCreateRecommendedOperator(
            [NotNullWhen(true)] out ICrossover<RealVector>? recommendation)
        {
            recommendation = new SinglePointCrossover();
            return true;
        }

        public bool TryCreateRecommendedOperator(
            [NotNullWhen(true)] out IMutator<RealVector>? recommendation)
        {
            recommendation = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1);
            return true;
        }

        public override ObjectiveVector Evaluate(RealVector candidate, IRandomNumberGenerator random) => 0.0;
    }
}
