using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ISelector<TPhenotype, in TObjective> : IOperator {
  TPhenotype[] Select(TPhenotype[] population, TObjective[] objectives, int count);
}

public static class Selector {
  public static ISelector<TPhenotype, TObjective> Create<TPhenotype, TObjective>(Func<TPhenotype[], TObjective[], int, TPhenotype[]> selector) => new Selector<TPhenotype, TObjective>(selector);
}

public sealed class Selector<TPhenotype, TObjective> : ISelector<TPhenotype, TObjective> {
  private readonly Func<TPhenotype[], TObjective[], int, TPhenotype[]> selector;
  internal Selector(Func<TPhenotype[], TObjective[], int, TPhenotype[]> selector) {
    this.selector = selector;
  }
  public TPhenotype[] Select(TPhenotype[] population, TObjective[] objectives, int count) => selector(population, objectives, count);
}

public abstract class SelectorBase<TSolution, TObjective> : ISelector<TSolution, TObjective> {
  public abstract TSolution[] Select(TSolution[] population, TObjective[] objectives, int count);
}

public class ProportionalSelector<TSolution> : ISelector<TSolution, ObjectiveValue> {
  public ProportionalSelector(RandomSource randomSource) {
    RandomSource = randomSource;
  }
  public RandomSource RandomSource { get; }

  public TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
    for (int j = 0; j < count; j++) {
      double totalQuality = objectives.Sum(o => o.Value);
      double randomValue = rng.Random() * totalQuality;
      double cumulativeQuality = 0.0;

      for (int i = 0; i < population.Length(); i++) {
        cumulativeQuality += objectives[i].Value;
        if (cumulativeQuality >= randomValue) {
          selected[j] = population[i];
          break;
        }
      }
    }
    return selected;
  }
}

public class RandomSelector<TSolution, TObjective> : SelectorBase<TSolution, TObjective> {
  public RandomSelector(RandomSource randomSource) {
    RandomSource = randomSource;
  }
  public RandomSource RandomSource { get; }

  public override TSolution[] Select(TSolution[] population, TObjective[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
    for (int i = 0; i < count; i++) {
      int index = rng.Integer(population.Length);
      selected[i] = population[index];
    }
    return selected;
  }
}

public class TournamentSelector<TSolution> : SelectorBase<TSolution, ObjectiveValue> where TSolution : class {
  public TournamentSelector(int tournamentSize, RandomSource randomSource) {
    TournamentSize = tournamentSize;
    RandomSource = randomSource;
  }
  
  public int TournamentSize { get; }
  public RandomSource RandomSource { get; }

  public override TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
    var populationIndexMap = population
      .Select((solution, index) => (solution, index))
      .ToDictionary(x => x.solution, x => x.index);

    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<TSolution>();
      for (int j = 0; j < TournamentSize; j++) {
        int index = rng.Integer(population.Length);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => objectives[populationIndexMap[participant]]).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }
}
