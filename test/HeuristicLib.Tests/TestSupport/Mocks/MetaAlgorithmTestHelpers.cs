using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public static class MetaAlgorithmTestHelpers
{
    public static IProblem<int, DummySearchSpace<int>> CreateIntegerProblem()
    {
        return FuncProblem.Create<int, DummySearchSpace<int>>(
          evaluateFunc: x => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
    }

    public static int StateCandidate(PopulationState<int> state) => state.Population.EvaluatedCandidates.Single().Candidate;

    public static double StateObjective(PopulationState<int> state) => state.Population.EvaluatedCandidates.Single().ObjectiveVector[0];
}
