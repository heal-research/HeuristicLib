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
  public bool IsTerminal(Permutation genotype, IRandomNumberGenerator random) => genotype.Count == innerProblem.ProblemData.NumberOfCities;

  public ObjectiveVector Bound(Permutation genotype, IRandomNumberGenerator random)
  {
    var pd = innerProblem.ProblemData;
    var totalDistance = 0.0;
    for (var i = 0; i < genotype.Count - 1; i++) {
      totalDistance += pd.GetDistance(genotype[i], genotype[i + 1]);
    }

    //TODO this is only valid for metric spaces
    totalDistance += pd.GetDistance(genotype[^1], genotype[0]); // Return to the starting city 

    return totalDistance;
  }

  public ObjectiveVector? EvaluatePartial(Permutation genotypes, IRandomNumberGenerator random) => !IsTerminal(genotypes, random) ? null : innerProblem.Evaluate(genotypes, random);
  public override ObjectiveVector Evaluate(Permutation genotypes, IRandomNumberGenerator random) => innerProblem.Evaluate(genotypes, random);

  public Permutation ApplyChoice(Permutation genotype, int choice) => new(genotype.Append(choice));
}
