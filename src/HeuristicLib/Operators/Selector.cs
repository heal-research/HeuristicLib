using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;


public interface ISelectorOperator : IExecutableOperator {
  Phenotype<TGenotype>[] Select<TGenotype>(Phenotype<TGenotype>[] population, Objective objective, int count);
}

public abstract class SelectorOperatorBase : ISelectorOperator {
  public abstract Phenotype<TGenotype>[] Select<TGenotype>(Phenotype<TGenotype>[] population, Objective objective, int count);
}

public class ProportionalSelectorOperator : SelectorOperatorBase { // ToDo: Probability-based selection base class
  public IRandomNumberGenerator Random { get; }
  public bool Windowing { get; }

  public ProportionalSelectorOperator(IRandomNumberGenerator random, bool windowing = true) {
    Random = random;
    Windowing = windowing;
  }
  
  public override Phenotype<TGenotype>[] Select<TGenotype>(Phenotype<TGenotype>[] population, Objective objective, int count) {
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
    var selected = new Phenotype<TGenotype>[count];
    for (int i = 0; i < count; i++) {
      double selectedQuality = Random.Random() * qualitySum;
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

public class RandomSelectorOperator : SelectorOperatorBase {
  public IRandomNumberGenerator Random { get; }
  
  public RandomSelectorOperator(IRandomNumberGenerator random) {
    Random = random;
  }
  
  public override Phenotype<TGenotype>[] Select<TGenotype>(Phenotype<TGenotype>[] population, Objective objective, int count) {
    var selected = new Phenotype<TGenotype>[count];
    for (int i = 0; i < count; i++) {
      int index = Random.Integer(population.Length);
      selected[i] = population[index];
    }
    return selected;
  }
}

public class TournamentSelectorOperator : SelectorOperatorBase {
  public IRandomNumberGenerator Random { get; }
  public int TournamentSize { get; }
  
  public TournamentSelectorOperator(int tournamentSize, IRandomNumberGenerator random) {
    Random = random;
    TournamentSize = tournamentSize;
  }

  public override Phenotype<TGenotype>[] Select<TGenotype>(Phenotype<TGenotype>[] population, Objective objective, int count) {
    var selected = new Phenotype<TGenotype>[count];
 
    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<Phenotype<TGenotype>>();
      for (int j = 0; j < TournamentSize; j++) {
        int index = Random.Integer(population.Length);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => participant.Fitness, objective.TotalOrderComparer).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }
}
