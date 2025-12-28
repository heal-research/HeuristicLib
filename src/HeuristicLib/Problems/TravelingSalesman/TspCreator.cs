using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TspCreator : Creator<Permutation, PermutationEncoding, TravelingSalesmanProblem> {
  public override Permutation Create(IRandomNumberGenerator random, PermutationEncoding encoding, TravelingSalesmanProblem problem) {
    var problemData = problem.ProblemData;
    var destinationsSortedToFirstCities = Enumerable
                                          .Range(1, problemData.NumberOfCities - 1)
                                          .Select(destination => (destination, distance: problemData.GetDistance(0, destination)))
                                          .OrderBy(x => x.distance)
                                          .Select(x => x.destination);

    return new Permutation([0, .. destinationsSortedToFirstCities]);
  }
}
