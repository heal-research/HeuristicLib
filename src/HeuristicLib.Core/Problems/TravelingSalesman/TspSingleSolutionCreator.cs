using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TspSingleSolutionCreator : StatelessSingleSolutionCreator<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
{
  public override Permutation Create(IRandomNumberGenerator random, PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem)
  {
    var problemData = problem.ProblemData;
    var destinationsSortedToFirstCities = Enumerable
      .Range(1, problemData.NumberOfCities - 1)
      .Select(destination => (destination, distance: problemData.GetDistance(0, destination)))
      .OrderBy(x => x.distance)
      .Select(x => x.destination);

    return new Permutation([0, .. destinationsSortedToFirstCities]);
  }
}
