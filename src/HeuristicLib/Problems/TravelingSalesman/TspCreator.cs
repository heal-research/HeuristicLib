using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public record class TspCreator : Creator<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
  public override TspCreatorInstance CreateExecution(PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem) {
    return new TspCreatorInstance(this, searchSpace, problem);
  }
}

public class TspCreatorInstance : CreatorExecution<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, TspCreator> {
  public TspCreatorInstance(TspCreator parameters, PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem) : base(parameters, searchSpace, problem) { }
  
  public override Permutation Create(IRandomNumberGenerator random) {
    var problemData = Problem.ProblemData;
    var destinationsSortedToFirstCities = Enumerable
      .Range(1, problemData.NumberOfCities - 1)
      .Select(destination => (destination, distance: problemData.GetDistance(0, destination)))
      .OrderBy(x => x.distance)
      .Select(x => x.destination);

    return new Permutation([0, .. destinationsSortedToFirstCities]);
  }
}
