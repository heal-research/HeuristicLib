using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Scenarios.Extensions.Problems.Dynamic.TravelingSalesman;

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
    var ga = GeneticAlgorithm.GetBuilder(
      new RandomPermutationCreator(),
      new EdgeRecombinationCrossover(),
      new InversionMutator()
    );

    //ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
    //ga.RandomSeed = 42;
    ga.PopulationSize = 100;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<Permutation>(2);
    ga.Elites = 1;
    //ga.Evaluator = prob.WrapEvaluator(ga.Evaluator);

    //prob.AttachTo(ga);

    //execute
    var resGa = ga.Build().WithMaxIterations(1000).RunToCompletion(prob, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

    //look at results
    var objGa = resGa.Population
                     .OrderBy(x => x.ObjectiveVector[0])
                     .First();

    resGa.Population.Solutions.Length.ShouldBe(100);
    resGa.Population.Solutions.All(solution => prob.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
    resGa.Population.Solutions.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
    double.IsFinite(objGa.ObjectiveVector[0]).ShouldBeTrue();
    objGa.ObjectiveVector[0].ShouldBeGreaterThan(0.0);
  }
}