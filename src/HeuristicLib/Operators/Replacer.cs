using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TGenotype> : IOperator {
  (TGenotype[] newPopulation, ObjectiveValue[] newObjectives) Replace(
    TGenotype[] previousPopulation, ObjectiveValue[] previousObjectives,
    TGenotype[] offspringPopulation, ObjectiveValue[] offspringObjectives);
  int GetOffspringCount(int populationSize);
}

public static class Replacer {
  public static IReplacer<TGenotype> Create<TGenotype>(Func<TGenotype[], ObjectiveValue[], TGenotype[], ObjectiveValue[], (TGenotype[], ObjectiveValue[])> replacer, Func<int, int> populationCount) => new Replacer<TGenotype>(replacer, populationCount);
}

public sealed class Replacer<TGenotype> : IReplacer<TGenotype> {
  private readonly Func<TGenotype[], ObjectiveValue[], TGenotype[], ObjectiveValue[], (TGenotype[], ObjectiveValue[])> replacer;
  private readonly Func<int, int> offspringCount;
  
  internal Replacer(Func<TGenotype[], ObjectiveValue[], TGenotype[], ObjectiveValue[], (TGenotype[], ObjectiveValue[])> replacer, Func<int, int> offspringCount) {
    this.replacer = replacer;
    this.offspringCount = offspringCount;
  }
  public (TGenotype[] newPopulation, ObjectiveValue[] newObjectives) Replace(TGenotype[] previousPopulation, ObjectiveValue[] previousObjectives, TGenotype[] offspringPopulation, ObjectiveValue[] offspringObjectives) => replacer(previousPopulation, previousObjectives, offspringPopulation, offspringObjectives);
  public int GetOffspringCount(int populationSize) => offspringCount(populationSize);
}


public abstract class ReplacerBase<TSolution> : IReplacer<TSolution> {
  public abstract int GetOffspringCount(int populationSize);
  public abstract (TSolution[] newPopulation, ObjectiveValue[] newObjectives) Replace(TSolution[] previousPopulation, ObjectiveValue[] previousObjectives, TSolution[] offspringPopulation, ObjectiveValue[] offspringObjectives);
}

public class PlusSelectionReplacer<TSolution> : ReplacerBase<TSolution>
{
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }

  public override (TSolution[] newPopulation, ObjectiveValue[] newObjectives) Replace(
    TSolution[] previousPopulation, ObjectiveValue[] previousObjectives,
    TSolution[] offspringPopulation, ObjectiveValue[] offspringObjectives)
  {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    var combinedQualities = previousObjectives.Concat(offspringObjectives).ToList();

    var sortedIndices = combinedQualities
      .Select((objective, index) => new { objective, index })
      .OrderBy(x => x.objective)
      .Select(x => x.index)
      .ToList();

    // if algorithmPopulation differs from previousPopulation.Length, it is not detected
    var newPopulation = sortedIndices.Take(previousPopulation.Length).Select(i => combinedPopulation[i]).ToArray();
    var newQualities = sortedIndices.Take(previousPopulation.Length).Select(i => combinedQualities[i]).ToArray();

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
    TSolution[] previousPopulation, ObjectiveValue[] previousObjectives,
    TSolution[] offspringPopulation, ObjectiveValue[] offspringObjectives)
  {
    var sortedPreviousIndices = previousObjectives
      .Select((quality, index) => new { quality, index })
      .OrderBy(x => x.quality)
      .Select(x => x.index)
      .ToList();

    var elitesPopulation = sortedPreviousIndices.Take(Elites).Select(i => previousPopulation[i]).ToList();
    var elitesQualities = sortedPreviousIndices.Take(Elites).Select(i => previousObjectives[i]).ToList();

    var newPopulation = elitesPopulation.Concat(offspringPopulation).ToArray();
    var newQualities = elitesQualities.Concat(offspringObjectives).ToArray();

    return (newPopulation, newQualities);
  }
}
