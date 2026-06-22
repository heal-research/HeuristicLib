using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record RandomSelector<TGenotype>
  : StatelessSelector<TGenotype>
{
    public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
      => RandomSelector.Select(population, count, random);
}

public static class RandomSelector
{
    public static IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(IReadOnlyList<Solution<TGenotype>> population, int count, IRandomNumberGenerator random)
    {
        var selected = new Solution<TGenotype>[count];
        var randoms = random.NextInts(selected.Length, population.Count);
        for (var i = 0; i < selected.Length; i++)
        {
            var index = randoms[i];
            selected[i] = population[index];
        }

        return selected;
    }
}
