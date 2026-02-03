using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.TravelingSalesman;

public class ActivatedTravelingSalesmanProblem : DynamicProblem<Permutation, PermutationSearchSpace> {
  public ActivatedTravelingSalesmanProblem(ITravelingSalesmanProblemData tspData,
                                           IRandomNumberGenerator environmentRandom,
                                           double activationProb = 0.9,
                                           double switchProbability = 0.1,
                                           UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                           int epochLength = int.MaxValue) : base(environmentRandom, updatePolicy, epochLength) {
    SwitchProbability = switchProbability;
    CurrentState = Generate(tspData, activationProb, environmentRandom);
    SearchSpace = new PermutationSearchSpace(tspData.NumberOfCities);
    Objective = SingleObjective.Minimize;
    ProblemData = tspData;
  }

  public ActivatedTravelingSalesmanProblem(ITravelingSalesmanProblemData tspData,
                                           IRandomNumberGenerator environmentRandom,
                                           bool[] startState,
                                           double switchProbability = 0.1,
                                           UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                           int epochLength = int.MaxValue) : base(environmentRandom, updatePolicy, epochLength) {
    SwitchProbability = switchProbability;
    ArgumentOutOfRangeException.ThrowIfNotEqual(tspData.NumberOfCities, startState.Length);
    CurrentState = startState.ToArray();
    SearchSpace = new PermutationSearchSpace(tspData.NumberOfCities);
    Objective = SingleObjective.Minimize;
    ProblemData = tspData;
  }

  public IReadOnlyList<bool> CurrentState { get; private set; }
  public double SwitchProbability { get; init; }
  public ITravelingSalesmanProblemData ProblemData { get; }
  public override PermutationSearchSpace SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    return solution
           .Where(x => CurrentState[x])
           .PairwiseRoundRobin(ProblemData.GetDistance)
           .Sum();
  }

  protected override void Update() {
    var active = new List<int>();
    var inactive = new List<int>();

    for (int i = 0; i < CurrentState.Count; i++) {
      if (CurrentState[i]) active.Add(i);
      else inactive.Add(i);
    }

    int k = Math.Min(active.Count, inactive.Count);
    if (k == 0) return;

    ShuffleInPlace(active, EnvironmentRandom);
    ShuffleInPlace(inactive, EnvironmentRandom);

    var next = CurrentState.ToArray();

    for (int i = 0; i < k; i++) {
      if (!EnvironmentRandom.Boolean(SwitchProbability))
        continue;
      int a = active[i];
      int b = inactive[i];
      next[a] = false;
      next[b] = true;
    }

    CurrentState = next;
  }

// Fisher–Yates shuffle
  private static void ShuffleInPlace<T>(IList<T> list, IRandomNumberGenerator rng) {
    for (int i = list.Count - 1; i > 0; i--) {
      int j = rng.Integer(0, i, true);
      (list[i], list[j]) = (list[j], list[i]);
    }
  }

  private static bool[] Generate(ITravelingSalesmanProblemData tspData, double activationProb, IRandomNumberGenerator random) {
    return Enumerable.Range(0, tspData.NumberOfCities).Select(_ => random.Boolean(activationProb)).ToArray();
  }
}
