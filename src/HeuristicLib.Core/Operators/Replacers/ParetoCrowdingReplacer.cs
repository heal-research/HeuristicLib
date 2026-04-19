using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record ParetoCrowdingReplacer<TGenotype>
  : StatelessReplacer<TGenotype>
{
  private readonly bool dominateOnEqualities;

  public ParetoCrowdingReplacer(bool dominateOnEqualities)
  {
    this.dominateOnEqualities = dominateOnEqualities;
  }

  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random)
    => ParetoCrowdingReplacer.Replace(previousPopulation, offspringPopulation, objective, count, dominateOnEqualities);
}

public static class ParetoCrowdingReplacer
{
  public static IReadOnlyList<ISolution<TGenotype>> Replace<TGenotype>(
    IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation,
    Objective objective,
    int count,
    bool dominateOnEqualities)
  {
    var all = previousPopulation.Concat(offspringPopulation).ToArray();
    var fronts = DominationCalculator.CalculateAllParetoFronts(all, objective, out _, dominateOnEqualities);

    var l = new List<ISolution<TGenotype>>();
    var size = count;
    foreach (var front in fronts) {
      if (front.Count < size) {
        l.AddRange(front);
        size -= front.Count;

        continue;
      }

      var dist = CrowdingDistance.CalculateCrowdingDistances(front.Select(x => x.ObjectiveVector).ToList());
      l.AddRange(front.Select((x, i) => (x, i)).OrderByDescending(x => dist[x.i]).Select(x => x.x).Take(size));

      break;
    }

    return l;
  }
}
