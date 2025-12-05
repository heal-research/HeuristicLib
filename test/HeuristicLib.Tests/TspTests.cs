using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Encodings.Permutation.Creators;
using HEAL.HeuristicLib.Encodings.Permutation.Crossovers;
using HEAL.HeuristicLib.Encodings.Permutation.Mutators;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

#pragma warning disable S1481

namespace HEAL.HeuristicLib.Tests;

public class TspTests {
  [Fact]
  public void LoadTSPLIB() {
    foreach (var f in Directory.EnumerateFiles(@"D:\Projekte\HCAI\HeuristicLib\Python\tsp\", "*.tsp")) {
      var data = TsplibTspInstanceProvider.LoadData(f);
      var prob = new TravelingSalesmanProblem((data.Coordinates?.Length ?? 0) <= 1000 ? data.ToDistanceMatrixData() : data.ToCoordinatesData());
    }
  }

  [Fact]
  public void GaWithTSP() {
    //Load Problem
    var data = TsplibTspInstanceProvider.LoadData(@"C:\Users\P41603\Downloads\berlin52.tsp");
    var cdata = data.ToCoordinatesData();
    var prob = new TravelingSalesmanProblem(cdata);

    //GA
    var ga = GeneticAlgorithm.GetBuilder(
      creator: new RandomPermutationCreator(),
      crossover: new EdgeRecombinationCrossover(),
      mutator: new InversionMutator()
    );

    ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
    ga.RandomSeed = 42;
    ga.PopulationSize = 100;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<Permutation>(2);
    ga.Elites = 1;

    //execute
    var resGa = ga.Execute(prob);

    //look at results
    var objGa = resGa.Population
                     .OrderBy(x => x.ObjectiveVector[0])
                     .First();

    //best possible 7542
  }

  [Fact]
  public void GaWithDynamicTSP() {
    //Load Problem
    var data = TsplibTspInstanceProvider.LoadData(@"C:\Users\P41603\Downloads\berlin52.tsp");
    var cdata = data.ToCoordinatesData();
    var prob = new ActivatedTravelingSalesmanProblem(cdata, new SystemRandomNumberGenerator(0), epochLength: 10000);

    //GA
    var ga = GeneticAlgorithm.GetBuilder(
      creator: new RandomPermutationCreator(),
      crossover: new EdgeRecombinationCrossover(),
      mutator: new InversionMutator()
    );

    ga.Terminator = new AfterIterationsTerminator<Permutation>(1000);
    ga.RandomSeed = 42;
    ga.PopulationSize = 100;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<Permutation>(2);
    ga.Elites = 1;
    ga.Evaluator = prob.WrapEvaluator(ga.Evaluator);

    prob.AttachTo(ga);

    //execute
    var resGa = ga.Execute(prob);

    //look at results
    var objGa = resGa.Population
                     .OrderBy(x => x.ObjectiveVector[0])
                     .First();

    //best possible 7542
  }
}
