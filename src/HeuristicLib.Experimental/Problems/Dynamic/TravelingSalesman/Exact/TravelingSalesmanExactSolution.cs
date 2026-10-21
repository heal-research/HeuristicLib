using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record TravelingSalesmanExactSolution(ImmutableArray<int> Tour, double Quality);

public interface ITravelingSalesmanExactSolver
{
    TravelingSalesmanExactSolution Solve(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> cities,
                                         CancellationToken cancellationToken = default);
}

public static class TravelingSalesmanExactSolverExtensions
{
    extension(ITravelingSalesmanExactSolver solver)
    {
        public TravelingSalesmanExactSolution Solve(ActivatedTravelingSalesmanProblem problem, CancellationToken cancellationToken = default) =>
            solver.Solve(problem.ProblemData, problem.ActiveCities, cancellationToken);
    }

    public static double EvaluateTour(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> tour)
    {
        if (tour.Count <= 1)
            return 0.0;

        var quality = 0.0;
        for (var i = 0; i < tour.Count; i++)
        {
            quality += problemData.GetDistance(tour[i], tour[(i + 1) % tour.Count]);
        }

        return quality;
    }
}
