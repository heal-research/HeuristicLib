using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record GeneralizedRankSelector<TCandidate>(double Pressure) : StatelessSelector<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
      => GeneralizedRankSelector.Select(population, objective, count, random, Pressure);
}

public static class GeneralizedRankSelector
{
    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
      ObjectiveDirections objective,
      int count,
      IRandomNumberGenerator random,
      double pressure)
    {
        var selected = new EvaluatedCandidate<TCandidate>[count];
        var source = population.OrderBy(x => x.ObjectiveVector, objective.TotalOrderComparer).ToArray();
        var scale = Math.Pow(population.Count, 1.0 / pressure) - 1;
        for (var i = 0; i < count; i++)
        {
            var rand = 1 + random.NextDouble() * scale;
            var selIdx = (int)Math.Floor(Math.Pow(rand, pressure) - 1);
            selected[i] = source[selIdx];
        }

        return selected;
    }
}
