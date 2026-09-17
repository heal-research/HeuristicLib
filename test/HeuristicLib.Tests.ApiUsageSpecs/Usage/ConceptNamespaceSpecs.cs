using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using IntegerCreator = HEAL.HeuristicLib.Encodings.IntegerVectors.UniformDistributedCreator;
using RealCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

public sealed class ConceptNamespaceSpecs
{
    [Fact]
    public void CandidateNamespaces_ExposeCandidatesSearchSpacesAndOperators()
    {
        new PermutationSearchSpace(3).Contains(new Permutation(0, 1, 2)).ShouldBeTrue();
        new BoundedRealVectorSearchSpace(2, -1.0, 1.0).Contains(new RealVector(0.0, 0.5)).ShouldBeTrue();
        new IntegerVectorSearchSpace(2, -1, 1).Contains(new IntegerVector(0, 1)).ShouldBeTrue();
        new BoolVectorSearchSpace(2).Contains(new BoolVector(true, false)).ShouldBeTrue();

        new RandomPermutationCreator().ShouldNotBeNull();
        new RealCreator().ShouldNotBeNull();
        new IntegerCreator().ShouldNotBeNull();
    }

    [Fact]
    public void SymbolicExpressionNamespace_OwnsAuthoringExecutionFormattingAndMetrics()
    {
        var expression = InfixExpressionParser.Parse("x + 1");

        expression.EvaluateSingleRow(new Dictionary<string, double> { ["x"] = 2.0 }).ShouldBe(3.0);
        expression.ToInfixString().ShouldNotBeNullOrEmpty();
        ExpressionMetrics.Length.Evaluate(expression).ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public async Task TravelingSalesmanFlow_UsesTheProblemAndPermutationNamespaces()
    {
        var problem = new TravelingSalesmanProblem();
        var algorithm = GeneticAlgorithm.For(problem, populationSize: 8, maximumGenerations: 1);

        var result = await algorithm.CompleteAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Count.ShouldBe(8);
    }

    [Fact]
    public async Task Nsga2Flow_UsesTheAlgorithmNamespaceAndObjectivePolicy()
    {
        var problem = new MultiObjectiveTestFunctionProblem(new Zdt1(4));
        var algorithm = NSGA2.Create(
            new RealCreator(problem.SearchSpace),
            new SimulatedBinaryCrossover(),
            new PolynomialMutator(),
            populationSize: 8,
            maximumGenerations: 1);

        var result = await algorithm.CompleteAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Count.ShouldBe(8);
        result.Population.All(candidate => candidate.ObjectiveVector.Count == 2).ShouldBeTrue();
    }
}
