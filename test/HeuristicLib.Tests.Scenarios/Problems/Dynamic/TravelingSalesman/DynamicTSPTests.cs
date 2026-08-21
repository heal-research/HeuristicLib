using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic.TravelingSalesman;

public class DynamicTSPTests
{
    [Fact]
    public void GaWithDynamicTSP()
    {
        //Load Problem
        var file = Path.Combine("TestData", "berlin52.tsp");
        var data = TsplibTspInstanceProvider.LoadData(file);
        var cdata = data.ToCoordinatesData();
        var prob = new ActivatedTravelingSalesmanProblem(cdata, RandomNumberGenerator.Create(0), epochLength: 10000);

        //GA
        //ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
        var ga = GeneticAlgorithm.For(
            prob.SearchSpace,
            creator: new RandomPermutationCreator(),
            crossover: new EdgeRecombinationCrossover(),
            mutator: new InversionMutator(),
            selector: TournamentSelector.For(prob, tournamentSize: 2),
            populationSize: 100,
            mutationRate: 0.05,
            elites: 1);
        //ga.Evaluator = prob.WrapEvaluator(ga.Evaluator);

        //prob.AttachTo(ga);

        //execute
        var resGa = (ga with { MaximumGenerations = 1000 }).Complete(prob, RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken);

        //look at results
        var objGa = resGa.Population
                         .OrderBy(x => x.ObjectiveVector[0])
                         .First();

        resGa.Population.EvaluatedCandidates.Count.ShouldBe(100);
        resGa.Population.EvaluatedCandidates.All(solution => prob.SearchSpace.Contains(solution.Candidate))
             .ShouldBeTrue();
        resGa.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
        double.IsFinite(objGa.ObjectiveVector[0]).ShouldBeTrue();
        objGa.ObjectiveVector[0].ShouldBeGreaterThan(0.0);
    }
}
