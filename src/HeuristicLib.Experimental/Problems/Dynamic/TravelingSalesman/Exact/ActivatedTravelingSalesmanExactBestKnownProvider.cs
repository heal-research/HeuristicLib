using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed class ActivatedTravelingSalesmanExactBestKnownProvider(ITravelingSalesmanExactSolver exactSolver)
    : IBestKnownObjectiveProvider<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>
{
    public ObjectiveVector GetBestKnown(ActivatedTravelingSalesmanProblem problem) => exactSolver.Solve(problem).Quality;
}
