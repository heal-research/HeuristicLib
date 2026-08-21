using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Problems;

public class TravelingSalesmanGeneticAlgorithmTests
{
    [Fact]
    public void GaWithDefaultTsp_Complete_ReturnsPopulationWithinProblemSearchSpace()
    {
        var problem = TravelingSalesmanProblem.CreateDefault();
        var ga = GeneticAlgorithm.For(
          problem,
          creator: new RandomPermutationCreator(),
          crossover: new OrderCrossover(),
          mutator: new InversionMutator(),
          selector: RandomSelector.For(problem),
          populationSize: 5,
          maximumGenerations: 5,
          mutationRate: 0.5,
          elites: 0);

        var result = ga
                       .Complete(
                         problem,
                         RandomNumberGenerator.Create(42),
                         ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Count.ShouldBe(5);
        result.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
        result.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
    }
}
