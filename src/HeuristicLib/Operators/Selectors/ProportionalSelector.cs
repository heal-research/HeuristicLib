using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record ProportionalSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public bool Windowing { get; init; } = true;

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        ProportionalSelector.Select(population, objective, count, random, Windowing);
}

public static class ProportionalSelector
{
    public static ProportionalSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, bool windowing = true)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new() { Windowing = windowing };

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, bool windowing = true)
    {
        var singleObjective = objective.Directions.Length == 1 ? objective.Directions[0] : throw new InvalidOperationException("Proportional selection requires a single objective.");
        var fitnesses = population.Select(s => s.ObjectiveVector.Count == 1 ? s.ObjectiveVector[0] : throw new InvalidOperationException("Proportional selection requires a single objective.")).ToList();

        // prepare qualities
        // The window is taken over the ordered values only. NaN is the worst possible value, and including it would
        // make every selection weight NaN rather than just its own.
        double minQuality = double.PositiveInfinity, maxQuality = double.NegativeInfinity;
        foreach (var val in fitnesses)
        {
            if (double.IsNaN(val))
            {
                continue;
            }

            minQuality = Math.Min(minQuality, val);
            maxQuality = Math.Max(maxQuality, val);
        }

        var list = new double[fitnesses.Count];
        if (minQuality > maxQuality)
        {
            // Every fitness was NaN, so nothing can be ranked and the draw falls back to a uniform one.
            Array.Fill(list, 1.0);
        }
        else if (minQuality == maxQuality || Math.Abs(minQuality - maxQuality) < double.Epsilon)
        {
            for (var i = 0; i < list.Length; i++)
            {
                list[i] = double.IsNaN(fitnesses[i]) ? 0.0 : 1.0;
            }
        }
        else if (windowing)
        {
            for (var i = 0; i < list.Length; i++)
            {
                var quality = singleObjective == ObjectiveDirection.Maximize
                    ? fitnesses[i] - minQuality
                    : maxQuality - fitnesses[i];

                // NaN, and the equal infinities that a window of infinite width subtracts to NaN, both describe a
                // candidate at the worst end of the population. It gets no share of the wheel.
                list[i] = double.IsNaN(quality) ? 0.0 : quality;
            }
        }
        else
        {
            if (minQuality < 0.0)
            {
                throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
            }

            var limit = Math.Min(maxQuality * 2, double.MaxValue);
            for (var i = 0; i < list.Length; i++)
            {
                var quality = singleObjective == ObjectiveDirection.Minimize ? limit - fitnesses[i] : fitnesses[i];

                // An infinitely bad fitness drives the minimized form below zero, which is not a share of anything.
                list[i] = double.IsNaN(quality) ? 0.0 : Math.Max(0.0, quality);
            }
        }

        // Proportional selection is undefined once a share is infinite. In the limit the infinite entries take the
        // whole wheel, so the draw is restricted to them and spread uniformly.
        if (Array.Exists(list, double.IsPositiveInfinity))
        {
            for (var i = 0; i < list.Length; i++)
            {
                list[i] = double.IsPositiveInfinity(list[i]) ? 1.0 : 0.0;
            }
        }

        // Rescale by the largest share. Shares derived from very large fitnesses otherwise sum to infinity, and an
        // infinite sum turns the draw itself into infinity or, for a draw of zero, into NaN.
        var maxShare = 0.0;
        foreach (var share in list)
        {
            maxShare = Math.Max(maxShare, share);
        }

        if (maxShare > 0.0 && double.IsFinite(maxShare))
        {
            for (var i = 0; i < list.Length; i++)
            {
                list[i] /= maxShare;
            }
        }

        var qualitySum = list.Sum();

        var selected = new EvaluatedCandidate<TCandidate>[count];
        for (var i = 0; i < selected.Length; i++)
        {
            var selectedQuality = random.NextDouble() * qualitySum;
            var index = 0;
            var currentQuality = list[index];

            // Advance past every entry whose cumulative share has not yet passed the draw. The comparison has to be
            // inclusive: a candidate with no share leaves the cumulative sum unchanged and must be stepped over.
            while (currentQuality <= selectedQuality && index < list.Length - 1)
            {
                index++;
                currentQuality += list[index];
            }

            selected[i] = population[index];
        }

        return selected;
    }
}
