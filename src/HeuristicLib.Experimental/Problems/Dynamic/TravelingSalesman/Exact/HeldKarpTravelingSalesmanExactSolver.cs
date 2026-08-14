using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed class HeldKarpTravelingSalesmanExactSolver(int maximumCities = 18) : ITravelingSalesmanExactSolver
{
    public int MaximumCities { get; } = maximumCities;

    public TravelingSalesmanExactSolution Solve(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> cities,
                                                CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(MaximumCities);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cities.Count, MaximumCities);

        return cities.Count switch
        {
            0 => new TravelingSalesmanExactSolution([], 0.0),
            1 => new TravelingSalesmanExactSolution([cities[0]], 0.0),
            2 => SolveTwoCityProblem(problemData, cities),
            _ => SolveHeldKarp(problemData, cities, cancellationToken)
        };
    }

    private static TravelingSalesmanExactSolution SolveTwoCityProblem(ITravelingSalesmanProblemData problemData,
                                                                      IReadOnlyList<int> cities)
    {
        var tour = ImmutableArray.Create(cities[0], cities[1]);
        return new TravelingSalesmanExactSolution(tour,
            TravelingSalesmanExactSolverExtensions.EvaluateTour(problemData, tour));
    }

    private static TravelingSalesmanExactSolution SolveHeldKarp(ITravelingSalesmanProblemData problemData,
                                                                IReadOnlyList<int> cities,
                                                                CancellationToken cancellationToken)
    {
        var cityCount = cities.Count;
        var stateCount = 1 << cityCount;
        var startMask = 1;
        var fullMask = stateCount - 1;
        var costs = new double[stateCount, cityCount];
        var parents = new int[stateCount, cityCount];

        for (var mask = 0; mask < stateCount; mask++)
        {
            for (var city = 0; city < cityCount; city++)
            {
                costs[mask, city] = double.PositiveInfinity;
                parents[mask, city] = -1;
            }
        }

        costs[startMask, 0] = 0.0;

        for (var mask = 0; mask < stateCount; mask++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ((mask & startMask) == 0)
            {
                continue;
            }

            for (var last = 0; last < cityCount; last++)
            {
                var currentCost = costs[mask, last];
                if (double.IsPositiveInfinity(currentCost))
                {
                    continue;
                }

                for (var next = 1; next < cityCount; next++)
                {
                    var nextBit = 1 << next;
                    if ((mask & nextBit) != 0)
                    {
                        continue;
                    }

                    var nextMask = mask | nextBit;
                    var candidateCost = currentCost + problemData.GetDistance(cities[last], cities[next]);
                    if (candidateCost >= costs[nextMask, next])
                    {
                        continue;
                    }

                    costs[nextMask, next] = candidateCost;
                    parents[nextMask, next] = last;
                }
            }
        }

        var bestLast = -1;
        var bestQuality = double.PositiveInfinity;
        for (var last = 1; last < cityCount; last++)
        {
            var candidateQuality = costs[fullMask, last] + problemData.GetDistance(cities[last], cities[0]);
            if (candidateQuality >= bestQuality)
            {
                continue;
            }

            bestQuality = candidateQuality;
            bestLast = last;
        }

        var tour = new int[cityCount];
        var currentMask = fullMask;
        var current = bestLast;
        for (var position = cityCount - 1; position >= 1; position--)
        {
            tour[position] = cities[current];
            var parent = parents[currentMask, current];
            currentMask &= ~(1 << current);
            current = parent;
        }

        tour[0] = cities[0];

        return new TravelingSalesmanExactSolution(tour.ToImmutableArray(), bestQuality);
    }
}
