using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Tests.Scenarios.Core.Problems.TravelingSalesman;

public class TspScenarios
{
    [Fact]
    public void GaWithTSP()
    {
        // Load Problem
        var file = Path.Combine("TestData", "berlin52.tsp");
        var data = TsplibTspInstanceProvider.LoadData(file);
        var cdata = data.ToCoordinatesData();
        var prob = new TravelingSalesmanProblem(cdata);

        // GA
        // ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
        var ga = GeneticAlgorithm.For(
          prob,
          creator: new RandomPermutationCreator(),
          crossover: new EdgeRecombinationCrossover(),
          mutator: new InversionMutator(),
          selector: TournamentSelector.For(prob, tournamentSize: 2),
          populationSize: 100,
          maximumGenerations: 10,
          mutationRate: 0.05,
          elites: 1);

        // execute
        var resGa = ga.Complete(prob, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        // look at results
        var objGa = resGa.Population
                         .OrderBy(x => x.ObjectiveVector[0])
                         .First();

        resGa.Population.EvaluatedCandidates.Count.ShouldBe(100);
        resGa.Population.EvaluatedCandidates.All(solution => prob.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
        resGa.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
        double.IsFinite(objGa.ObjectiveVector[0]).ShouldBeTrue();
        objGa.ObjectiveVector[0].ShouldBeGreaterThan(0.0);
    }
}
