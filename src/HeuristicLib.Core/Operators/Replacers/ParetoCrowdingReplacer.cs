using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public class ParetoCrowdingReplacer<TGenotype>(bool dominateOnEqualities) : Replacer<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random)
  {
    var all = previousPopulation.Concat(offspringPopulation).ToArray();
    var fronts = DominationCalculator.CalculateAllParetoFronts(all, objective, out var rank, dominateOnEqualities);

    var l = new List<ISolution<TGenotype>>();
    var size = previousPopulation.Count;
    foreach (var front in fronts) {
      if (front.Count < size) {
        l.AddRange(front);
        size -= front.Count;
        continue;
      }

      var dist = CrowdingDistance.CalculateCrowdingDistances(front.Select(x => x.ObjectiveVector).ToList());
      l.AddRange(front.Select((x, i) => (x, i)).OrderBy(x => dist[x.i]).Select(x => x.x).Take(size));
      break;
    }

    return l;
  }

  public override int GetOffspringCount(int populationSize) => populationSize;
}
