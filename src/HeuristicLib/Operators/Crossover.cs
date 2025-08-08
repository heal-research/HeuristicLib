using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class Crossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class Crossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random, TEncoding encoding);

  TGenotype ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Cross(parent1, parent2, random, encoding);
  }
}

public abstract class Crossover<TGenotype> : ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random);

  TGenotype ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Cross(parent1, parent2, random);
  }
}


public class RandomCrossover<TGenotype> : Crossover<TGenotype>
{
  public double Bias { get; }
  public RandomCrossover(double bias = 0.5) {
    if (bias is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(bias), "Bias must be between 0 and 1.");
    Bias = bias;
    
  }
  
  public override TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random) {
    if (random.Random() < Bias) {
      return parent1;
    } else {
      return parent2;
    }
  }
}

public class MultiCrossover<TGenotype, TEncoding, TProblem> : Crossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public IReadOnlyList<ICrossover<TGenotype, TEncoding, TProblem>> Crossovers { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double totalWeightSum;
  
  public MultiCrossover(IReadOnlyList<ICrossover<TGenotype, TEncoding, TProblem>> crossovers, IReadOnlyList<double>? weights = null) {
    if (crossovers.Count == 0) throw new ArgumentException("At least one crossover must be provided.", nameof(crossovers));
    if (weights != null && weights.Count != crossovers.Count) throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
    if (weights != null && weights.Any(p => p < 0)) throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    if (weights != null && weights.All(p => p <= 0)) throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    
    Crossovers = crossovers;
    Weights = weights ?? Enumerable.Repeat(1.0, crossovers.Count).ToArray();
    totalWeightSum = Weights.Sum();
  }

  public override TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    // Select a crossover index based on the weights
    double r = random.Random() * totalWeightSum;
    double cumulative = 0.0;
    int selectedIndex = 0;
    for (int i = 0; i < Weights.Count; i++) {
      cumulative += Weights[i];
      if (r < cumulative) {
        selectedIndex = i;
        break;
      }
    }
    return Crossovers[selectedIndex].Cross(parent1, parent2, random, encoding, problem);
  }
}
