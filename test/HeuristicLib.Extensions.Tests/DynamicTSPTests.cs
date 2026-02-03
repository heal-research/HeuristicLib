using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class DynamicTSPTests
{
  private const string TestDataBerlin52TSP = @"TestData\berlin52.tsp";
  
  [Fact]
  public void GaWithDynamicTSP()
  {
    //Load Problem
    var data = TsplibTspInstanceProvider.LoadData(TestDataBerlin52TSP);
    var cdata = data.ToCoordinatesData();
    var prob = new ActivatedTravelingSalesmanProblem(cdata, RandomNumberGenerator.Create(0), epochLength: 10000);

    //GA
    var ga = GeneticAlgorithm.GetBuilder(
    new RandomPermutationCreator(),
    new EdgeRecombinationCrossover(),
    new InversionMutator()
    );

    ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
    //ga.RandomSeed = 42;
    ga.PopulationSize = 100;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<Permutation>(2);
    ga.Elites = 1;
    ga.Evaluator = prob.WrapEvaluator(ga.Evaluator);

    prob.AttachTo(ga);

    //execute
    var resGa = ga.Build().RunToCompletion(prob, RandomNumberGenerator.Create(42));

    //look at results
    var objGa = resGa.Population
      .OrderBy(x => x.ObjectiveVector[0])
      .First();

    //best possible 7542
  }
}
