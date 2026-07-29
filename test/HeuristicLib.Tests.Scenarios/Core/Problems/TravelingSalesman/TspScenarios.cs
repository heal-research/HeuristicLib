using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

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
        var ga = GeneticAlgorithm.GetBuilder(
          new RandomPermutationCreator(),
          new EdgeRecombinationCrossover(),
          new InversionMutator()
        );

        // ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
        // ga.RandomSeed = 42;
        ga.PopulationSize = 100;
        ga.MutationRate = 0.05;
        ga.Selector = TournamentSelector.For(prob, tournamentSize: 2);
        ga.Elites = 1;
        // execute
        var resGa = (ga.Build() with
        {
            MaximumGenerations = 10
        }).Complete(prob, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        // look at results
        var objGa = resGa.Population
                         .OrderBy(x => x.ObjectiveVector[0])
                         .First();

        resGa.Population.EvaluatedCandidates.Length.ShouldBe(100);
        resGa.Population.EvaluatedCandidates.All(solution => prob.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
        resGa.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
        double.IsFinite(objGa.ObjectiveVector[0]).ShouldBeTrue();
        objGa.ObjectiveVector[0].ShouldBeGreaterThan(0.0);
    }
}
