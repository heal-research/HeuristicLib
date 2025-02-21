namespace HEAL.HeuristicLib.Operators;




public interface IReplacer<TSolution>
{
  int GetOffspringCount(int populationSize);
  (IReadOnlyList<TSolution> newPopulation, IReadOnlyList<double> newQualities) Replace(
    IReadOnlyList<TSolution> previousPopulation, IReadOnlyList<double> previousQualities,
    IReadOnlyList<TSolution> offspringPopulation, IReadOnlyList<double> offspringQualities);
}

public class PlusSelectionReplacer<TSolution> : IReplacer<TSolution>
{
  public int GetOffspringCount(int populationSize) => populationSize;

  public (IReadOnlyList<TSolution> newPopulation, IReadOnlyList<double> newQualities) Replace(
    IReadOnlyList<TSolution> previousPopulation, IReadOnlyList<double> previousQualities,
    IReadOnlyList<TSolution> offspringPopulation, IReadOnlyList<double> offspringQualities)
  {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    var combinedQualities = previousQualities.Concat(offspringQualities).ToList();

    var sortedIndices = combinedQualities
      .Select((quality, index) => new { quality, index })
      .OrderBy(x => x.quality)
      .Select(x => x.index)
      .ToList();

    var newPopulation = sortedIndices.Take(previousPopulation.Count).Select(i => combinedPopulation[i]).ToList();
    var newQualities = sortedIndices.Take(previousPopulation.Count).Select(i => combinedQualities[i]).ToList();

    return (newPopulation, newQualities);
  }
}

public class ElitismReplacer<TSolution> : IReplacer<TSolution>
{
  private readonly int elites;

  public ElitismReplacer(int elites)
  {
    this.elites = elites;
  }

  public int GetOffspringCount(int populationSize) => populationSize - elites;

  public (IReadOnlyList<TSolution> newPopulation, IReadOnlyList<double> newQualities) Replace(
    IReadOnlyList<TSolution> previousPopulation, IReadOnlyList<double> previousQualities,
    IReadOnlyList<TSolution> offspringPopulation, IReadOnlyList<double> offspringQualities)
  {
    var sortedPreviousIndices = previousQualities
      .Select((quality, index) => new { quality, index })
      .OrderBy(x => x.quality)
      .Select(x => x.index)
      .ToList();

    var elitesPopulation = sortedPreviousIndices.Take(elites).Select(i => previousPopulation[i]).ToList();
    var elitesQualities = sortedPreviousIndices.Take(elites).Select(i => previousQualities[i]).ToList();

    var newPopulation = elitesPopulation.Concat(offspringPopulation).ToList();
    var newQualities = elitesQualities.Concat(offspringQualities).ToList();

    return (newPopulation, newQualities);
  }
}
