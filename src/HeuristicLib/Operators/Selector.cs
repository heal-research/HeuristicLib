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
  public IRandomSource RandomSource { get; }
  public bool Windowing { get; }

  public ProportionalSelector(IRandomSource randomSource, bool windowing = true) {
    RandomSource = randomSource;
    Windowing = windowing;
  }
  public TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();

    // prepare qualities
    double minQuality = double.MaxValue, maxQuality = double.MinValue;
    foreach (double val in objectives.Select(o => o.Value)) {
      minQuality = Math.Min(minQuality, val);
      maxQuality = Math.Max(maxQuality, val);
    }
    var direction = objectives.Select(o => o.Direction).Distinct().Single();

    var qualities = objectives.Select(o => o.Value);
    if (Math.Abs(minQuality - maxQuality) < double.Epsilon) {
      qualities = qualities.Select(_ => 1.0);
    } else {
      if (Windowing) {
        if (direction == ObjectiveDirection.Maximize) {
          qualities = qualities.Select(q => q - minQuality);
        } else {
          qualities = qualities.Select(q => maxQuality - q);
        }
      } else {
        if (minQuality < 0.0)
          throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
        if (direction == ObjectiveDirection.Minimize) {
          double limit = Math.Min(maxQuality * 2, double.MaxValue);
          qualities = qualities.Select(q => limit - q);
        }
      }
    }

    var list = qualities.ToList();
    double qualitySum = list.Sum();
    var selected = new TSolution[count];
    for (int i = 0; i < count; i++) {
      double selectedQuality = rng.Random() * qualitySum;
      int index = 0;
      double currentQuality = list[index];
      while (currentQuality < selectedQuality) {
        index++;
        currentQuality += list[index];
      }
      selected[i] = population[index];
    }
    return selected;
  }
}

public class RandomSelector<TSolution, TObjective> : SelectorBase<TSolution, TObjective> {
  public RandomSelector(IRandomSource randomSource) {
    RandomSource = randomSource;
  }
  public IRandomSource RandomSource { get; }

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

public class TournamentSelector<TSolution> : SelectorBase<TSolution, ObjectiveValue> {
  // Custom comparator for tournament selection
  public int TournamentSize { get; }
  public IRandomSource RandomSource { get; }
  
  public TournamentSelector(int tournamentSize, IRandomSource randomSource) {
    TournamentSize = tournamentSize;
    RandomSource = randomSource;
  }

  public override TSolution[] Select(TSolution[] population, ObjectiveValue[] objectives, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new TSolution[count];
#pragma warning disable CS8714
    var populationIndexMap = population
      .Select((solution, index) => (solution, index))
      .ToDictionary(x => x.solution, x => x.index);
#pragma warning restore CS8714
 
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
