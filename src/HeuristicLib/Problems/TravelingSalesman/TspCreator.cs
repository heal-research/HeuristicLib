using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TspCreator : Creator<Permutation, PermutationEncoding, TravelingSalesmanProblem> {
  public override Permutation Create(IExecutionContext<PermutationEncoding, TravelingSalesmanProblem> context) {
    var problem = context.Problem;
    var problemData = problem.ProblemData;
    var destinationsSortedToFirstCities = Enumerable
      .Range(1, problemData.NumberOfCities - 1)
      .Select(destination => (destination, distance: problemData.GetDistance(0, destination)))
      .OrderBy(x => x.distance)
      .Select(x => x.destination);

    return new Permutation([0, .. destinationsSortedToFirstCities]);
  }
}
