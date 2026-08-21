using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record ParetoCrowdingTournamentSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public int TournamentSize { get; init; } = 2;
    public bool DominateOnEqualities { get; init; }

    public ParetoCrowdingTournamentSelector(bool dominateOnEqualities)
    {
        DominateOnEqualities = dominateOnEqualities;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        ParetoCrowdingTournamentSelector.Select(population, objective, count, random, DominateOnEqualities, TournamentSize);
}

public static class ParetoCrowdingTournamentSelector
{
    public static ParetoCrowdingTournamentSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, bool dominateOnEqualities, int tournamentSize = 2)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(dominateOnEqualities) { TournamentSize = tournamentSize };

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, bool dominateOnEqualities, int tournamentSize = 2)
    {
        var fronts = DominationCalculator.CalculateAllParetoFronts(population, objective, out var rank, dominateOnEqualities);

        // Key by solution instead of ObjectiveVector
        var crowdingBySolution = new Dictionary<EvaluatedCandidate<TCandidate>, double>(ReferenceEqualityComparer.Instance);
        var res = new EvaluatedCandidate<TCandidate>[count];

        var calculatedFront = new HashSet<int>();

        for (var i = 0; i < count; i++)
        {
            var bestIdx = random.NextInt(population.Count);
            var bestRank = rank[bestIdx];

            for (var j = 1; j < tournamentSize; j++)
            {
                var idx = random.NextInt(population.Count);
                var idxRank = rank[idx];
                if (idxRank < bestRank)
                {
                    bestIdx = idx;
                    bestRank = rank[bestIdx];
                    continue;
                }

                if (idxRank > bestRank)
                {
                    continue; // worse rank
                }

                // equal rank -> compare crowding
                // ensure we have distances for this front
                if (!calculatedFront.Contains(bestRank))
                {
                    var frontSolutions = fronts[bestRank];
                    var frontObjectives = frontSolutions.Select(x => x.ObjectiveVector).ToArray();
                    var distances = CrowdingDistance.CalculateCrowdingDistances(frontObjectives);
                    for (var k = 0; k < frontSolutions.Count; k++)
                    {
                        crowdingBySolution[frontSolutions[k]] = distances[k];
                    }

                    calculatedFront.Add(bestRank);
                }

                if (crowdingBySolution[population[idx]] <= crowdingBySolution[population[bestIdx]])
                {
                    continue;
                }

                bestIdx = idx;
                bestRank = idxRank;
            }

            res[i] = population[bestIdx];
        }

        return res;
    }
}
