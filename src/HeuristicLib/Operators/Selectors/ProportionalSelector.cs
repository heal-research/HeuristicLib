using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record ProportionalSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public ProportionalSelector(bool windowing = true)
    {
        Windowing = windowing;
    }

    public bool Windowing { get; init; }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        ProportionalSelector.Select(population, objective, count, random, Windowing);
}

public static class ProportionalSelector
{
    public static ProportionalSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, bool windowing = true)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(windowing);

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, bool windowing = true)
    {
        var singleObjective = objective.Directions.Length == 1 ? objective.Directions[0] : throw new InvalidOperationException("Proportional selection requires a single objective.");
        var fitnesses = population.Select(s => s.ObjectiveVector.Count == 1 ? s.ObjectiveVector[0] : throw new InvalidOperationException("Proportional selection requires a single objective.")).ToList();

        // prepare qualities
        double minQuality = double.MaxValue, maxQuality = double.MinValue;
        foreach (var val in fitnesses)
        {
            minQuality = Math.Min(minQuality, val);
            maxQuality = Math.Max(maxQuality, val);
        }

        var qualities = fitnesses.AsEnumerable();
        if (Math.Abs(minQuality - maxQuality) < double.Epsilon)
        {
            qualities = qualities.Select(_ => 1.0);
        }
        else
        {
            if (windowing)
            {
                qualities = singleObjective == ObjectiveDirection.Maximize ? qualities.Select(q => q - minQuality) : qualities.Select(q => maxQuality - q);
            }
            else
            {
                if (minQuality < 0.0)
                {
                    throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
                }

                if (singleObjective == ObjectiveDirection.Minimize)
                {
                    var limit = Math.Min(maxQuality * 2, double.MaxValue);
                    qualities = qualities.Select(q => limit - q);
                }
            }
        }

        var list = qualities.ToArray();
        var qualitySum = list.Sum();

        var selected = new EvaluatedCandidate<TCandidate>[count];
        for (var i = 0; i < selected.Length; i++)
        {
            var selectedQuality = random.NextDouble() * qualitySum;
            var index = 0;
            var currentQuality = list[index];
            while (currentQuality < selectedQuality)
            {
                index++;
                currentQuality += list[index];
            }

            selected[i] = population[index];
        }

        return selected;
    }
}
