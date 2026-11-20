using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record GeneralizedRankSelector<TCandidate>(double Pressure) : StatelessSelector<TCandidate>
{
    public static Parameter<GeneralizedRankSelector<TCandidate>, double> PressureParameter { get; } =
        new(nameof(Pressure), static selector => selector.Pressure, static (selector, value) => selector with { Pressure = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [PressureParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        GeneralizedRankSelector.Select(population, objective, count, random, Pressure);
}

public static class GeneralizedRankSelector
{
    public static GeneralizedRankSelector<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, double pressure) => new(pressure);

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, double pressure)
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
