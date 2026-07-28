using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record RandomSelector<TCandidate>
  : StatelessSelector<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
      => RandomSelector.Select(population, count, random);
}

public static class RandomSelector
{
    public static RandomSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem) where TSearchSpace : class, ISearchSpace<TCandidate> => new();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, int count, IRandomNumberGenerator random)
    {
        var selected = new EvaluatedCandidate<TCandidate>[count];
        var randoms = random.NextInts(selected.Length, population.Count);
        for (var i = 0; i < selected.Length; i++)
        {
            var index = randoms[i];
            selected[i] = population[index];
        }

        return selected;
    }
}
