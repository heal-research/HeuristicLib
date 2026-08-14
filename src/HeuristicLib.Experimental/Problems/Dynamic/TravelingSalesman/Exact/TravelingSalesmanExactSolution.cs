using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record TravelingSalesmanExactSolution(ImmutableArray<int> Tour, double Quality);

public interface ITravelingSalesmanExactSolver
{
    TravelingSalesmanExactSolution Solve(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> cities,
                                         CancellationToken cancellationToken = default);
}
