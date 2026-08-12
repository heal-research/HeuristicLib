using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public static class TravelingSalesmanExactSolverExtensions
{
    public static TravelingSalesmanExactSolution Solve(this ITravelingSalesmanExactSolver solver,
                                                       ActivatedTravelingSalesmanProblem problem,
                                                       CancellationToken cancellationToken = default)
        => solver.Solve(problem.ProblemData, problem.ActiveCities, cancellationToken);

    public static double EvaluateTour(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> tour)
    {
        if (tour.Count <= 1)
        {
            return 0.0;
        }

        var quality = 0.0;
        for (var i = 0; i < tour.Count; i++)
        {
            quality += problemData.GetDistance(tour[i], tour[(i + 1) % tour.Count]);
        }

        return quality;
    }
}
