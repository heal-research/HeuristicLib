using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ISelector<TGenotype, TFitness, TGoal> : IOperator {
  Phenotype<TGenotype, TFitness>[] Select(Phenotype<TGenotype, TFitness>[] population, TGoal goal, int count);
}

public static class Selector {
  public static ISelector<TGenotype, TFitness, TGoal> Create<TGenotype, TFitness, TGoal>(
    Func<Phenotype<TGenotype, TFitness>[], TGoal, int, Phenotype<TGenotype, TFitness>[]> selector) => new Selector<TGenotype, TFitness, TGoal>(selector);
}

public sealed class Selector<TGenotype, TFitness, TGoal> : ISelector<TGenotype, TFitness, TGoal> {
  private readonly Func<Phenotype<TGenotype, TFitness>[], TGoal, int, Phenotype<TGenotype, TFitness>[]> selector;
  internal Selector(Func<Phenotype<TGenotype, TFitness>[], TGoal, int, Phenotype<TGenotype, TFitness>[]> selector) {
    this.selector = selector;
  }
  public Phenotype<TGenotype, TFitness>[] Select(Phenotype<TGenotype, TFitness>[] population, TGoal goal, int count) => selector(population, goal, count);
}

public abstract class SelectorBase<TGenotype, TFitness, TGoal> : ISelector<TGenotype, TFitness, TGoal> {
  public abstract Phenotype<TGenotype, TFitness>[] Select(Phenotype<TGenotype, TFitness>[] population, TGoal goal, int count);
}

public class ProportionalSelector<TGenotype> : SelectorBase<TGenotype, Fitness, Goal> {
  public IRandomSource RandomSource { get; }
  public bool Windowing { get; }

  public ProportionalSelector(IRandomSource randomSource, bool windowing = true) {
    RandomSource = randomSource;
    Windowing = windowing;
  }
  public override Phenotype<TGenotype, Fitness>[] Select(Phenotype<TGenotype, Fitness>[] population, Goal goal, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();

    // prepare qualities
    double minQuality = double.MaxValue, maxQuality = double.MinValue;
    foreach (double val in population.Select(p => p.Fitness.Value)) {
      minQuality = Math.Min(minQuality, val);
      maxQuality = Math.Max(maxQuality, val);
    }
    
    var qualities = population.Select(p => p.Fitness!.Value);
    if (Math.Abs(minQuality - maxQuality) < double.Epsilon) {
      qualities = qualities.Select(_ => 1.0);
    } else {
      if (Windowing) {
        if (goal == Goal.Maximize) {
          qualities = qualities.Select(q => q - minQuality);
        } else {
          qualities = qualities.Select(q => maxQuality - q);
        }
      } else {
        if (minQuality < 0.0)
          throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
        if (goal == Goal.Minimize) {
          double limit = Math.Min(maxQuality * 2, double.MaxValue);
          qualities = qualities.Select(q => limit - q);
        }
      }
    }

    var list = qualities.ToList();
    double qualitySum = list.Sum();
    var selected = new Phenotype<TGenotype, Fitness>[count];
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

  public class Factory : IOperatorFactory<ISelector<TGenotype, Fitness, Goal>>, IStochasticOperatorFactory {
    private readonly bool windowing;
    private IRandomSource? randomSource;
    
    public Factory(bool windowing = true) {
      this.windowing = windowing;
    }
    
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;
    
    public ISelector<TGenotype, Fitness, Goal> Create() {
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new ProportionalSelector<TGenotype>(randomSource, windowing);
    }
  }
}

public class RandomSelector<TGenotype, TFitness, TGoal> : SelectorBase<TGenotype, TFitness, TGoal> {
  public IRandomSource RandomSource { get; }
  
  public RandomSelector(IRandomSource randomSource) {
    RandomSource = randomSource;
  }
  
  public override Phenotype<TGenotype, TFitness>[] Select(Phenotype<TGenotype, TFitness>[] population, TGoal goal, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var selected = new Phenotype<TGenotype, TFitness>[count];
    for (int i = 0; i < count; i++) {
      int index = rng.Integer(population.Length);
      selected[i] = population[index];
    }
    return selected;
  }

  public class Factory : IOperatorFactory<ISelector<TGenotype, Fitness, Goal>>, IStochasticOperatorFactory {
    private IRandomSource? randomSource;
    
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;
    
    public ISelector<TGenotype, Fitness, Goal> Create() {
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new RandomSelector<TGenotype, Fitness, Goal>(randomSource);
    }
  }
}

public class TournamentSelector<TGenotype> : SelectorBase<TGenotype, Fitness, Goal> {
  public int TournamentSize { get; }
  public IRandomSource RandomSource { get; }
  //public IComparer<TFitness> Comparer { get; }
  
  
  public TournamentSelector(int tournamentSize, IRandomSource randomSource/*, IComparer<TFitness> comparer*//*, */) {
    TournamentSize = tournamentSize;
    RandomSource = randomSource;
    //Comparer = comparer;
  }

  public override Phenotype<TGenotype, Fitness>[] Select(Phenotype<TGenotype, Fitness>[] population, Goal goal, int count) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    var comparer = Fitness.CreateSingleObjectiveComparer(goal);
    var selected = new Phenotype<TGenotype, Fitness>[count];
 
    for (int i = 0; i < count; i++) {
      var tournamentParticipants = new List<Phenotype<TGenotype, Fitness>>();
      for (int j = 0; j < TournamentSize; j++) {
        int index = rng.Integer(population.Length);
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.OrderBy(participant => participant.Fitness, comparer).First();
      selected[i] = bestParticipant;
    }
    return selected;
  }

  public class Factory : IOperatorFactory<ISelector<TGenotype, Fitness, Goal>>, IStochasticOperatorFactory {
    private readonly int tournamentSize;
    private IRandomSource? randomSource;
    
    public Factory(int? tournamentSize = null) {
      this.tournamentSize = tournamentSize ?? 2;
    }
    
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;
    
    public ISelector<TGenotype, Fitness, Goal> Create() {
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new TournamentSelector<TGenotype>(tournamentSize, randomSource);
    }
  }
}
