using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Extensions.Tests.Dynamic;

public static class QuadraticAssignmentProblemHelper
{
  public static QuadraticAssignmentProblemData CreateDefaultA()
  {
    // Example flow and distance matrices for a small QAP instance
    var flowMatrix = new double[,] { { 0, 2, 0, 1 }, { 2, 0, 3, 0 }, { 0, 3, 0, 4 }, { 1, 0, 4, 0 } };
    var distanceMatrix = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };

    return new QuadraticAssignmentProblemData(flowMatrix, distanceMatrix);
  }

  public static QuadraticAssignmentProblemData CreateDefaultB()
  {
    // Example flow and distance matrices for a small QAP instance
    var flowMatrix = new double[,] { { 0, 0, 0, 0 }, { 1, 0, 0, 0 }, { 1, 1, 0, 0 }, { 1, 1, 1, 0 } };
    var distanceMatrix = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };

    return new QuadraticAssignmentProblemData(flowMatrix, distanceMatrix);
  }
}

public class QuadraticAssignmentProblemTests
{
  [Fact]
  public void Evaluate_KnownToyInstance_ReturnsExpectedCost()
  {
    var problem = QuadraticAssignmentProblem.CreateDefault();
    var rng = RandomNumberGenerator.System(1);

    // identity assignment: facility i -> location i
    var sol = new Permutation([0, 1, 2, 3]);// adapt ctor if needed

    var cost = problem.Evaluate(sol, rng)[0];// adapt cast if ObjectiveVector differs

    // Compute expected manually for the default matrices you used
    // expected = sum_i sum_j F[i,j] * D[i,j]
    const double expected = 0 * 0 + 2 * 1 + 0 * 2 + 1 * 3
                            + 2 * 1 + 0 * 0 + 3 * 1 + 0 * 2
                            + 0 * 2 + 3 * 1 + 0 * 0 + 4 * 1
                            + 1 * 3 + 0 * 2 + 4 * 1 + 0 * 0;

    Assert.Equal(expected, cost, 10);
  }

  [Fact]
  public void Alpha0_EqualsInstanceA()
  {
    var rng = RandomNumberGenerator.System(1);
    var a = QuadraticAssignmentProblemHelper.CreateDefaultA();
    var b = QuadraticAssignmentProblemHelper.CreateDefaultB();
    var dyn = new InterpolatedQuadraticAssignmentProblem(a, b, rng, 0.0, 0.1, true);
    var sol = new Permutation(Enumerable.Range(0, a.Size).ToArray());
    var costDyn = dyn.Evaluate(sol, RandomNumberGenerator.NoRandom);
    var costA = new QuadraticAssignmentProblem(a).Evaluate(sol, RandomNumberGenerator.NoRandom);
    Assert.Equal(costA, costDyn);
  }

  [Fact]
  public void Alpha1_EqualsInstanceB_AfterSteps()
  {
    var rng = RandomNumberGenerator.System(1);
    var a = QuadraticAssignmentProblemHelper.CreateDefaultA();
    var b = QuadraticAssignmentProblemHelper.CreateDefaultB();
    var dyn = new InterpolatedQuadraticAssignmentProblem(a, b, rng, 0.0, 1.0, true, false);

    dyn.UpdateOnce();
    var sol = new Permutation(Enumerable.Range(0, a.Size).ToArray());
    var costDyn = dyn.Evaluate(sol, RandomNumberGenerator.NoRandom);
    var costB = new QuadraticAssignmentProblem(b).Evaluate(sol, RandomNumberGenerator.NoRandom);
    Assert.Equal(costB, costDyn);
  }

  [Fact]
  public void SigmaZero_EqualsBaseCost_Always()
  {
    var baseData = QuadraticAssignmentProblemHelper.CreateDefaultA();
    var env = RandomNumberGenerator.System(123);

    var dyn = new NoisyFlowQuadraticAssignmentProblem(baseData, env, 0.0);
    var stat = new QuadraticAssignmentProblem(baseData);

    var sol = new Permutation(Enumerable.Range(0, baseData.Size).ToArray());
    var c1 = dyn.Evaluate(sol, RandomNumberGenerator.NoRandom);
    var c2 = stat.Evaluate(sol, RandomNumberGenerator.NoRandom);

    Assert.Equal(c2, c1);

    // After many updates, still equal (since noise is zero)
    for (var i = 0; i < 50; i++) {
      dyn.EpochClock.AdvanceEpoch();
    }

    var c3 = dyn.Evaluate(sol, RandomNumberGenerator.NoRandom);
    Assert.Equal(c2, c3);
  }
}
