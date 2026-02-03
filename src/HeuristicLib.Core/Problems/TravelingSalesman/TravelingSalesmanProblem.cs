using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TravelingSalesmanProblem(ITravelingSalesmanProblemData problemData) : PermutationProblem(SingleObjective.Minimize, GetEncoding(problemData))/*, IDeterministicProblem<Permutation>*/
{

  public TravelingSalesmanProblem() : this(new TravelingSalesmanCoordinatesData(DefaultProblemCoordinates)) {}
  public ITravelingSalesmanProblemData ProblemData { get; } = problemData;

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random)
  {
    var totalDistance = 0.0;
    for (var i = 0; i < solution.Count - 1; i++) {
      totalDistance += ProblemData.GetDistance(solution[i], solution[i + 1]);
    }

    totalDistance += ProblemData.GetDistance(solution[^1], solution[0]);// Return to the starting city

    return totalDistance;
  }

  private static PermutationSearchSpace GetEncoding(ITravelingSalesmanProblemData problemData) => new(problemData.NumberOfCities);

  #region Default Instance

  public static TravelingSalesmanProblem CreateDefault()
  {
    var problemData = new TravelingSalesmanCoordinatesData(DefaultProblemCoordinates);

    return new TravelingSalesmanProblem(problemData);
  }

  private static readonly double[,] DefaultProblemCoordinates = new double[,] { { 100, 100 }, { 100, 200 }, { 100, 300 }, { 100, 400 }, { 200, 100 }, { 200, 200 }, { 200, 300 }, { 200, 400 }, { 300, 100 }, { 300, 200 }, { 300, 300 }, { 300, 400 }, { 400, 100 }, { 400, 200 }, { 400, 300 }, { 400, 400 } };

  #endregion

}
