using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.Problems.Dynamic.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Dynamic;

public class TravelingSalesmanProblemTests {
  static readonly double[,] D = { { 0, 1, 2, 3 }, { 1, 0, 4, 5 }, { 2, 4, 0, 6 }, { 3, 5, 6, 0 } };

  [Fact]
  public void Update_SwitchProbZero_StateUnchanged() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0);
    var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, false], switchProbability: 0.0);

    var before = p.CurrentState.ToArray();
    p.UpdateOnce();
    Assert.Equal(before, p.CurrentState);
  }

  [Fact]
  public void Evaluate_AllActive_EqualsFullCycleCost() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0); // irrelevant for evaluation
    var p = new ActivatedTravelingSalesmanProblem(data, env, activationProb: 1.0, switchProbability: 0.0);
    Assert.Equal([true, true, true, true], p.CurrentState);
    var tour = new Permutation([0, 1, 2, 3]);
    var cost = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    // Full cycle: 0->1->2->3->0 = 1 + 4 + 6 + 3 = 14
    Assert.Equal(14.0, cost, 10);
    p.UpdateOnce();
    var cost1 = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    Assert.Equal(14.0, cost1, 10);
  }

  [Fact]
  public void Evaluate_SkipsInactiveCities_ReconnectsTour() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0); // irrelevant for evaluation
    var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, true], switchProbability: 0.0);
    var tour = new Permutation([0, 1, 2, 3]);
    var cost = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    // 0->2 (2) + 2->3 (6) + 3->0 (3) = 11
    Assert.Equal(11.0, cost, 10);
    p.UpdateOnce();
    var cost1 = p.Evaluate(tour, env)[0];
    Assert.Equal(11.0, cost1, 10);
  }

  [Fact]
  public void Evaluate_NoActiveCities_ReturnsZero() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0); // irrelevant for evaluation
    var p = new ActivatedTravelingSalesmanProblem(data, env, [false, false, false, false], switchProbability: 0.0);
    var tour = new Permutation([0, 1, 2, 3]);

    var cost = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    Assert.Equal(0.0, cost, 10);
    p.UpdateOnce();
    var cost1 = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    Assert.Equal(0.0, cost1, 10);
  }

  [Fact]
  public void Evaluate_OneActiveCity_ReturnsZero() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0); // irrelevant for evaluation
    var p = new ActivatedTravelingSalesmanProblem(data, env, [false, true, false, false], switchProbability: 0.0);

    var tour = new Permutation([0, 1, 2, 3]);

    var cost = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    Assert.Equal(0.0, cost, 10);
    p.UpdateOnce();
    var cost1 = p.Evaluate(tour, NoRandomGenerator.Instance)[0];
    Assert.Equal(0.0, cost1, 10);
  }

  [Fact]
  public void Update_SwitchProbOne_FlipsAllBits() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0); // irrelevant for evaluation
    var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, false], switchProbability: 1.0);
    p.UpdateOnce();
    Assert.Equal([false, true, false, true], p.CurrentState);
  }

  [Fact]
  public void Evaluate_TwoActiveCities_IsTwoWayEdgeSum() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0);
    var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, false, true], switchProbability: 0.0);

    var tour = new Permutation([0, 1, 2, 3]);
    var cost = p.Evaluate(tour, NoRandomGenerator.Instance)[0];

    // filtered tour: [0,3] => 0->3 (3) + 3->0 (3) = 6
    Assert.Equal(6.0, cost, 10);
  }

  [Fact]
  public void MyCachedTestCase() {
    var data = new TravelingSalesmanDistanceMatrixProblemData(D);
    var env = new SystemRandomNumberGenerator(0);
    var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, false, true], switchProbability: 1.0, epochLength: 200);
    var tour = new Permutation([0, 1, 2, 3]);
    var cachedEval = p.GetCachedEvaluator<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>();
    Assert.Equal(0, p.EpochClock.CurrentEpoch);

    var r1 = cachedEval.Evaluate([tour], NoRandomGenerator.Instance, p.SearchSpace, p)[0];
    var r2 = cachedEval.Evaluate([tour], NoRandomGenerator.Instance, p.SearchSpace, p)[0];
    Assert.Equal(r1.ToArray(), r2.ToArray());

    p.UpdateOnce();
    Assert.Equal(1, p.EpochClock.CurrentEpoch);

    var r3 = cachedEval.Evaluate([tour], NoRandomGenerator.Instance, p.SearchSpace, p)[0];

    Assert.NotNull(r3);
  }
}
