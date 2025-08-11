using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ISelector<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}


public abstract class BatchSelector<TGenotype, TEncoding, TProblem> : ISelector<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchSelector<TGenotype, TEncoding> : ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding);
  
  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Select(population, objective, count, random, encoding);
  }
}

public abstract class BatchSelector<TGenotype> : ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);
  
  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Select(population, objective, count, random);
  }
}


public abstract class Selector<TGenotype, TEncoding, TProblem> : ISelector<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract Solution<TGenotype> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);


  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, TEncoding, TProblem>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var selected = new Solution<TGenotype>[count];
    Parallel.For(0, selected.Length, i => {
      selected[i] = Select(population, objective, random, encoding, problem);
    });
    return selected;
  }
}

public abstract class Selector<TGenotype, TEncoding> : ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract Solution<TGenotype> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    var selected = new Solution<TGenotype>[count];
    Parallel.For(0, selected.Length, i => {
      selected[i] = Select(population, objective, random, encoding);
    });
    return selected;
  }
}

public abstract class Selector<TGenotype> : ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract Solution<TGenotype> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, IEncoding<TGenotype> encoding);

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    var selected = new Solution<TGenotype>[count];
    Parallel.For(0, selected.Length, i => {
      selected[i] = Select(population, objective, random, encoding);
    });
    return selected;
  }
}



public class ProportionalSelector<TGenotype> : BatchSelector<TGenotype>
{ // ToDo: Probability-based selection base class (fitness -> probability, rank -> probability, etc.)
  public bool Windowing { get; set; }

  public ProportionalSelector(bool windowing = true) {
    Windowing = windowing;
  }

  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var singleObjective = objective.Directions.Length == 1 ? objective.Directions[0] : throw new InvalidOperationException("Proportional selection requires a single objective.");
    var fitnesses = population.Select(s => s.ObjectiveVector.Count == 1 ? s.ObjectiveVector[0] : throw new InvalidOperationException("Proportional selection requires a single objective.")).ToList();
    
    // prepare qualities
    double minQuality = double.MaxValue, maxQuality = double.MinValue;
    foreach (double val in fitnesses) {
      minQuality = Math.Min(minQuality, val);
      maxQuality = Math.Max(maxQuality, val);
    }
    
    var qualities = fitnesses.AsEnumerable();
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

    double[] list = qualities.ToArray();
    double qualitySum = list.Sum();
    
    var selected = new Solution<TGenotype>[count];
    for (int i = 0; i < selected.Length; i++) {
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

public class RandomSelector<TGenotype> : BatchSelector<TGenotype> 
{
  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype>[count];
    var randoms = random.Integers(population.Count, selected.Length);
    for (int i = 0; i < selected.Length; i++) {
      int index = randoms[i];
      selected[i] = population[index];
    }
    return selected;
  }
}

public class TournamentSelector<TGenotype> : BatchSelector<TGenotype>
{
  public int TournamentSize { get; set; }
  
  public TournamentSelector(int tournamentSize) {
    TournamentSize = tournamentSize;
  }
  
  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype>[count];
    var tournamentParticipants = new List<Solution<TGenotype>>(TournamentSize);
    for (int i = 0; i < selected.Length; i++) {
      tournamentParticipants.Clear();
      var randoms = random.Integers(population.Count, TournamentSize);
      for (int j = 0; j < TournamentSize; j++) {
        int index = randoms[j];
        tournamentParticipants.Add(population[index]);
      }
      var bestParticipant = tournamentParticipants.MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!;
      selected[i] = bestParticipant;
    }
    return selected;
  }
}
