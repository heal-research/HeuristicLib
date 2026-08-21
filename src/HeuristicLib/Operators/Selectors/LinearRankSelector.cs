using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record LinearRankSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        LinearRankSelector.Select(population, objective, count, random);
}

public static class LinearRankSelector
{
    public static LinearRankSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
    {
        var list = population.OrderByDescending(x => x.ObjectiveVector, objective.TotalOrderComparer).ToList();

        int lotSum = list.Count * (list.Count + 1) / 2;
        var selected = new EvaluatedCandidate<TCandidate>[count];
        for (int i = 0; i < count; i++)
        {
            int selectedLot = random.NextInt(lotSum);
            var index = (int)((Math.Sqrt(1 + 8 * selectedLot) - 1) / 2.0) + 1;
            selected[i] = list[index];
        }

        return selected;
    }
}
