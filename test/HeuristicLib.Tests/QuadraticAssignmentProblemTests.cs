//using HEAL.HeuristicLib.Encodings.Permutation;
//using HEAL.HeuristicLib.Problems.Dynamic;
//using HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;
//using HEAL.HeuristicLib.Problems.QuadraticAssignment;
//using HEAL.HeuristicLib.Random;

//namespace HEAL.HeuristicLib.Tests;

//public class QuadraticAssignmentProblemTests {
//  [Fact]
//  public void Evaluate_KnownToyInstance_ReturnsExpectedCost() {
//    var problem = QuadraticAssignmentProblem.CreateDefault();
//    var rng = new SystemRandomNumberGenerator(1);

//    // identity assignment: facility i -> location i
//    var sol = new Permutation([0, 1, 2, 3]); // adapt ctor if needed

//    var cost = problem.Evaluate(sol, rng)[0]; // adapt cast if ObjectiveVector differs

//    // Compute expected manually for the default matrices you used
//    // expected = sum_i sum_j F[i,j] * D[i,j]
//    const double expected = 0 * 0 + 2 * 1 + 0 * 2 + 1 * 3
//                            + 2 * 1 + 0 * 0 + 3 * 1 + 0 * 2
//                            + 0 * 2 + 3 * 1 + 0 * 0 + 4 * 1
//                            + 1 * 3 + 0 * 2 + 4 * 1 + 0 * 0;

//    Assert.Equal(expected, cost, 10);
//  }
//}
//public class InterpolatedQapTests {
//  [Fact]
//  public void Alpha0_EqualsInstanceA() {
//    var rng = new SystemRandomNumberGenerator(1);

//    var a = /* build data A */;
//    var b = /* build data B */;

//    var dyn = new InterpolatedQuadraticAssignmentProblem(
//      a, b, rng, alphaStart: 0.0, alphaStep: 0.1, interpolateDistances: true);

//    var sol = new Permutation(Enumerable.Range(0, a.Size).ToArray());

//    var costDyn = dyn.Evaluate(sol, rng);
//    var costA   = new QuadraticAssignmentProblem(a).Evaluate(sol, rng);

//    Assert.Equal(costA, costDyn);
//  }

//  [Fact]
//  public void Alpha1_EqualsInstanceB_AfterSteps() {
//    var rng = new SystemRandomNumberGenerator(1);

//    var a = /* build data A */;
//    var b = /* build data B */;

//    var dyn = new InterpolatedQuadraticAssignmentProblem(
//      a, b, rng, alphaStart: 0.0, alphaStep: 1.0, interpolateDistances: true, pingPong: false);

//    // Force update once (depends on base-class API; if Update is protected,
//    // you can trigger via whatever "tick" method DynamicProblem exposes in your framework,
//    // or make a small test subclass that exposes Update)
//    dyn.EpochClock.AdvanceEpoch();

//    var sol = new Permutation(Enumerable.Range(0, a.Size).ToArray());
//    var costDyn = dyn.Evaluate(sol, rng);
//    var costB   = new QuadraticAssignmentProblem(b).Evaluate(sol, rng);
//    Assert.Equal(costB, costDyn);
//  }

//  private void TriggerOneUpdate(InterpolatedQuadraticAssignmentProblem dyn) {
//    dyn.EpochClock.AdvanceEpoch();
//  }
//}
//public class NoisyFlowQapTests {
//  [Fact]
//  public void SigmaZero_EqualsBaseCost_Always() {
//    var baseData = /* build data */;
//    var env = new SystemRandomNumberGenerator(123);
//    var eval = new SystemRandomNumberGenerator(999);

//    var dyn = new NoisyFlowQuadraticAssignmentProblem(baseData, env, sigma: 0.0);
//    var stat = new QuadraticAssignmentProblem(baseData);

//    var sol = new Permutation(Enumerable.Range(0, baseData.Size).ToArray());

//    var c1 = dyn.Evaluate(sol, eval);
//    var c2 = stat.Evaluate(sol, eval);

//    Assert.Equal(c2, c1);

//    // After many updates, still equal (since noise is zero)
//    for (int i = 0; i < 50; i++)  dyn.EpochClock.AdvanceEpoch();

//    var c3 = dyn.Evaluate(sol, eval);
//    Assert.Equal(c2, c3);
//  }
//}


