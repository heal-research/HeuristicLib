using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed class ActivatedTravelingSalesmanExactBestKnownProvider(ITravelingSalesmanExactSolver exactSolver)
    : IBestKnownObjectiveProvider<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>
{
    public ObjectiveVector GetBestKnown(ActivatedTravelingSalesmanProblem problem)
        => exactSolver.Solve(problem).Quality;
}
