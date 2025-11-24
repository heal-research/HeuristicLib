using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Encodings.Permutation.Creators;
using HEAL.HeuristicLib.Encodings.Permutation.Crossovers;
using HEAL.HeuristicLib.Encodings.Permutation.Mutators;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;

#pragma warning disable S1481

namespace HEAL.HeuristicLib.Tests;

public class TspTests {
  [Fact]
  public void GaWithTSP() {
    //Load Problem
    var data = TsplibTspInstanceProvider.LoadData(@"C:\Users\P41603\Downloads\berlin52.tsp");
    var cdata = data.ToCoordinatesData();
    var prob = new TravelingSalesmanProblem(cdata);

    //GA
    var ga = GeneticAlgorithm.GetPrototype(
      creator: new RandomPermutationCreator(),
      crossover: new EdgeRecombinationCrossover(),
      mutator: new InversionMutator(),
      terminator: new AfterIterationsTerminator<Permutation>(1000),
      evaluator: prob.CreateEvaluator(),
      randomSeed: 42,
      populationSize: 100,
      mutationRate: 0.05,
      selector: new TournamentSelector<Permutation>(2),
      elites: 1
    );

    //execute
    var resGa = ga.Execute(prob);

    //look at results
    var objGa = resGa.Population
                     .OrderBy(x => x.ObjectiveVector[0])
                     .First();

    //best possible 7542
  }
}
