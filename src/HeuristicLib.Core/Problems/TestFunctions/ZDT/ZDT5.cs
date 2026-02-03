using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

/// <summary>
///   ZDT5 is a binary-encoded multi-objective optimization problem and therefore can not fit into the RealVector
///   TestFunctions
/// </summary>
public class Zdt5 : Problem<BoolVector, BoolVectorSearchSpace>
{
  private readonly int numberOfVariables;

  public Zdt5(int n) : base(MultiObjective.Minimize(2), new BoolVectorSearchSpace(n))
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(n, 35);
    ArgumentOutOfRangeException.ThrowIfNotEqual((n - 30) % 5, 0);
    numberOfVariables = (n - 30) / 5 + 1;
  }

  public override ObjectiveVector Evaluate(BoolVector solution, IRandomNumberGenerator random)
  {
    var g = G(solution);
    var f1 = F1(solution);

    return new ObjectiveVector(f1, g * H(f1, g));
  }

  private static double H(double f1, double d) => 1.0 / f1;

  private static double F1(BoolVector solution) => 1 + U(solution, 0);

  private double G(BoolVector solution)
  {
    var s = 0.0;
    for (var i = 1; i < numberOfVariables; i++) {
      var u = U(solution, i);
      s += u == 5 ? 1.0 : 2.0 + u;
    }

    return s;
  }

  private static int U(BoolVector solution, int varIdx)
  {
    var sum = 0;
    var start = varIdx == 0 ? 0 : 30 + (varIdx - 1) * 5;
    var end = start + (varIdx == 0 ? 30 : 5);
    for (var i = start; i < end; i++) {
      if (solution[i]) {
        sum++;
      }
    }

    return sum;
  }
}
