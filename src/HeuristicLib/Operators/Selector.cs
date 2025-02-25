using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

using System.Linq;


public interface ISelector<TSolution, TObjective>
{
  IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<TObjective> objectives, int count);
}



public class ProportionalSelector<TSolution> : ISelector<TSolution, ObjectiveValue>
{
  private readonly Random random = new();

  public IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<ObjectiveValue> objectives, int count)
  {
    var selected = new List<TSolution>();
    for (int j = 0; j < count; j++)
    {
      double totalQuality = objectives.Sum(o => o.Value);
      double randomValue = random.NextDouble() * totalQuality;
      double cumulativeQuality = 0.0;

      for (int i = 0; i < population.Count; i++)
      {
        cumulativeQuality += objectives[i].Value;
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

public class RandomSelector<TSolution, TObjective> : ISelector<TSolution, TObjective>
{
  private readonly Random random = new();

  public IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<TObjective> objectives, int count)
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

public class TournamentSelector<TSolution> : ISelector<TSolution, ObjectiveValue> where TSolution : notnull {
  private readonly Random random = new();
  private readonly int tournamentSize;

  public TournamentSelector(int tournamentSize)
  {
    this.tournamentSize = tournamentSize;
  }

public IReadOnlyList<TSolution> Select(IReadOnlyList<TSolution> population, IReadOnlyList<ObjectiveValue> objectives, int count)
  {
    var selected = new List<TSolution>();
    var populationIndexMap = population
      .Select((solution, index) => (solution, index))
      .ToDictionary(x => x.solution, x => x.index);

    for (int i = 0; i < count; i++)
    {
      var tournamentParticipants = new List<TSolution>();
      for (int j = 0; j < tournamentSize; j++)
      {
        int index = random.Next(population.Count);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => objectives[populationIndexMap[participant]]).First();
      selected.Add(bestParticipant);
    }
    return selected;
  }
}
