using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;


public interface ISelector : IOperator 
{
  Individual<TGenotype, TPhenotype>[] Select<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] population, Objective objective, int count, IRandomNumberGenerator random);
}

public abstract class SelectorBase : ISelector {
  public abstract Individual<TGenotype, TPhenotype>[] Select<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] population, Objective objective, int count, IRandomNumberGenerator random);
}

public class ProportionalSelector : SelectorBase { // ToDo: Probability-based selection base class
  public bool Windowing { get; }

  public ProportionalSelector(bool windowing = true) {
    Windowing = windowing;
  }
  
  public override Individual<TGenotype, TPhenotype>[] Select<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] population, Objective objective, int count, IRandomNumberGenerator random) {
    var singleObjective = objective.Directions.Length == 1 ? objective.Directions[0] : throw new InvalidOperationException("Proportional selection requires a single objective.");
    // prepare qualities
    double minQuality = double.MaxValue, maxQuality = double.MinValue;
    foreach (double val in population.Select(p => p.Fitness.SingleFitness!.Value.Value)) {
      minQuality = Math.Min(minQuality, val);
      maxQuality = Math.Max(maxQuality, val);
    }
    
    var qualities = population.Select(p => p.Fitness.SingleFitness!.Value.Value);
    if (Math.Abs(minQuality - maxQuality) < double.Epsilon) {
      qualities = qualities.Select(_ => 1.0);
    } else {
      if (Windowing) {
        if (singleObjective == ObjectiveDirection.Maximize) {
          qualities = qualities.Select(q => q - minQuality);
        } else {
          qualities = qualities.Select(q => maxQuality - q);
        }
      } else {
        if (minQuality < 0.0)
          throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
        if (singleObjective == ObjectiveDirection.Minimize) {
          double limit = Math.Min(maxQuality * 2, double.MaxValue);
          qualities = qualities.Select(q => limit - q);
        }
      }
    }

    var list = qualities.ToList();
    double qualitySum = list.Sum();
    var selected = new Individual<TGenotype, TPhenotype>[count];
    for (int i = 0; i < count; i++) {
      double selectedQuality = random.Random() * qualitySum;
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

public class RandomSelector : SelectorBase {
  public override Individual<TGenotype, TPhenotype>[] Select<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Individual<TGenotype, TPhenotype>[count];
    for (int i = 0; i < count; i++) {
      int index = random.Integer(population.Length);
      selected[i] = population[index];
    }
    return selected;
  }
}

public class TournamentSelector : SelectorBase {
  public int TournamentSize { get; }
  
  public TournamentSelector(int tournamentSize) {
    TournamentSize = tournamentSize;
  }

  public override Individual<TGenotype, TPhenotype>[] Select<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Individual<TGenotype, TPhenotype>[count];
    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<Individual<TGenotype, TPhenotype>>();
      for (int j = 0; j < TournamentSize; j++) {
        int index = random.Integer(population.Length);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => participant.Fitness, objective.TotalOrderComparer).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }
}
