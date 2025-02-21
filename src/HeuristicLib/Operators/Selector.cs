namespace HEAL.HeuristicLib.Operators;


public interface ISelector<TSolution>
{
  IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<double> qualities, int count);
}



public class ProportionalSelector<TSolution> : ISelector<TSolution>
{
  private readonly Random random = new();

  public IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<double> qualities, int count)
  {
    var selected = new List<TSolution>();
    for (int j = 0; j < count; j++)
    {
      double totalQuality = qualities.Sum();
      double randomValue = random.NextDouble() * totalQuality;
      double cumulativeQuality = 0.0;

      for (int i = 0; i < population.Count; i++)
      {
        cumulativeQuality += qualities[i];
        if (cumulativeQuality >= randomValue)
        {
          selected.Add(population[i]);
          break;
        }
      }
    }
    return selected;
  }
}

public class RandomSelector<TSolution> : ISelector<TSolution>
{
  private readonly Random random = new();

  public IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<double> qualities, int count)
  {
    var selected = new List<TSolution>();
    for (int i = 0; i < count; i++)
    {
      int index = random.Next(population.Count);
      selected.Add(population[index]);
    }
    return selected;
  }
}
