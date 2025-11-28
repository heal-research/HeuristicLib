using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public class ActivatedTravelingSalesmanProblem : DynamicProblem<Permutation, PermutationEncoding> {
  public ActivatedTravelingSalesmanProblem(ITravelingSalesmanProblemData tspData,
                                           IRandomNumberGenerator environmentRandom,
                                           double activationProb = 0.9,
                                           double switchProbability = 0.1,
                                           UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                           int epochLength = int.MaxValue) : base(updatePolicy, epochLength) {
    EnvironmentRandom = environmentRandom;
    SwitchProbability = switchProbability;
    CurrentState = Generate(tspData, activationProb, EnvironmentRandom);
    SearchSpace = new PermutationEncoding(tspData.NumberOfCities);
    Objective = SingleObjective.Minimize;
    ProblemData = tspData;
  }

  public IReadOnlyList<bool> CurrentState { get; private set; }
  private IRandomNumberGenerator EnvironmentRandom { get; }
  public double SwitchProbability { get; }
  public ITravelingSalesmanProblemData ProblemData { get; }
  public override PermutationEncoding SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    return solution
           .Where(x => CurrentState[x])
           .PairwiseRoundRobin(ProblemData.GetDistance)
           .Sum();
  }

  protected override void Update() {
    CurrentState = CurrentState
                   .Select(a => EnvironmentRandom.Boolean(SwitchProbability) ? !a : a)
                   .ToArray();
  }

  private static bool[] Generate(ITravelingSalesmanProblemData tspData, double activationProb, IRandomNumberGenerator random) {
    return Enumerable.Range(0, tspData.NumberOfCities).Select(_ => random.Boolean(activationProb)).ToArray();
  }
}
