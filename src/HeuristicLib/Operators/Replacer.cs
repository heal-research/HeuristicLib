using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TSolution> : IOperator {
  int GetOffspringCount(int populationSize);
  (TSolution[] newPopulation, ObjectiveValue[] newObjectives) Replace(
    TSolution[] previousPopulation, ObjectiveValue[] previousQualities,
    TSolution[] offspringPopulation, ObjectiveValue[] offspringQualities);
}

public abstract class ReplacerBase<TSolution> : IReplacer<TSolution> {
  public abstract int GetOffspringCount(int populationSize);
  public abstract (TSolution[] newPopulation, ObjectiveValue[] newObjectives) Replace(TSolution[] previousPopulation, ObjectiveValue[] previousQualities, TSolution[] offspringPopulation, ObjectiveValue[] offspringQualities);
}

public class PlusSelectionReplacer<TSolution> : ReplacerBase<TSolution>
{
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }

  public override (TSolution[] newPopulation, ObjectiveValue[] newObjectives) Replace(
    TSolution[] previousPopulation, ObjectiveValue[] previousQualities,
    TSolution[] offspringPopulation, ObjectiveValue[] offspringQualities)
  {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    var combinedQualities = previousQualities.Concat(offspringQualities).ToList();

    var sortedIndices = combinedQualities
      .Select((objective, index) => new { objective, index })
      .OrderBy(x => x.objective)
      .Select(x => x.index)
      .ToList();

    var newPopulation = sortedIndices.Take(previousPopulation.Length).Select(i => combinedPopulation[i]).ToArray();
    var newQualities = sortedIndices.Take(previousPopulation.Length()).Select(i => combinedQualities[i]).ToArray();

    return (newPopulation, newQualities);
  }
}

public class ElitismReplacer<TSolution> : ReplacerBase<TSolution> {
  public ElitismReplacer(int elites) {
    this.Elites = elites;
  }
  
  public int Elites { get; }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;

  public override (TSolution[] newPopulation, ObjectiveValue[] newObjectives) Replace(
    TSolution[] previousPopulation, ObjectiveValue[] previousQualities,
    TSolution[] offspringPopulation, ObjectiveValue[] offspringQualities)
  {
    var sortedPreviousIndices = previousQualities
      .Select((quality, index) => new { quality, index })
      .OrderBy(x => x.quality)
      .Select(x => x.index)
      .ToList();

    var elitesPopulation = sortedPreviousIndices.Take(Elites).Select(i => previousPopulation[i]).ToList();
    var elitesQualities = sortedPreviousIndices.Take(Elites).Select(i => previousQualities[i]).ToList();

    var newPopulation = elitesPopulation.Concat(offspringPopulation).ToArray();
    var newQualities = elitesQualities.Concat(offspringQualities).ToArray();

    return (newPopulation, newQualities);
  }
}
