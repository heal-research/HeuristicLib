using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

#pragma warning disable S1481

namespace HEAL.HeuristicLib.Tests.Problems;

public class TravelingSalesmanGeneticAlgorithmTests
{
    [Fact]
    public void GaWithDefaultTsp_Complete_ReturnsPopulationWithinProblemSearchSpace()
    {
        var problem = TravelingSalesmanProblem.CreateDefault();
        var ga = GeneticAlgorithm.GetBuilder(
          new RandomPermutationCreator(),
          new OrderCrossover(),
          new InversionMutator()
        );
        ga.PopulationSize = 5;
        ga.MutationRate = 0.5;
        ga.Selector = RandomSelector.For(problem);
        ga.Elites = 0;

        var result = (ga.Build() with
        {
            MaximumGenerations = 5
        })
                       .Complete(
                         problem,
                         RandomNumberGenerator.Create(42),
                         ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Length.ShouldBe(5);
        result.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
        result.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
    }
}
