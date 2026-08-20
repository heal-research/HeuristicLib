using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Partial;

public class TreeSearchTsp(TravelingSalesmanProblem innerProblem) :
  SingleSolutionProblem<Permutation, PermutationSearchSpace>(innerProblem.Objective, innerProblem.SearchSpace),
  IPartialSolutionProblem<Permutation, PermutationSearchSpace, int>
{
  public bool IsTerminal(Permutation candidate, IRandomNumberGenerator random) => candidate.Count == innerProblem.ProblemData.NumberOfCities;

  public ObjectiveVector Bound(Permutation candidate, IRandomNumberGenerator random)
  {
    var pd = innerProblem.ProblemData;
    var totalDistance = 0.0;
    for (var i = 0; i < candidate.Count - 1; i++) {
      totalDistance += pd.GetDistance(candidate[i], candidate[i + 1]);
    }

    //TODO this is only valid for metric spaces
    totalDistance += pd.GetDistance(candidate[^1], candidate[0]); // Return to the starting city

    return totalDistance;
  }

  public ObjectiveVector? EvaluatePartial(Permutation candidates, IRandomNumberGenerator random) => !IsTerminal(candidates, random) ? null : innerProblem.Evaluate(candidates, random);
  public override ObjectiveVector Evaluate(Permutation candidates, IRandomNumberGenerator random) => innerProblem.Evaluate(candidates, random);

  public Permutation ApplyChoice(Permutation candidate, int choice) => new(candidate.Append(choice));
}
