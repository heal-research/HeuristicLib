using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public interface ICrossover<TGenotype, in TEncoding> : ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
{
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICrossover<TGenotype> : ICrossover<TGenotype, IEncoding<TGenotype>>
{
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random);
}


public abstract class BatchCrossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchCrossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding);
  
  IReadOnlyList<TGenotype> ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Cross(parents, random, encoding);
  }
}

public abstract class BatchCrossover<TGenotype> : ICrossover<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Cross(parents, random);
  }
  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Cross(parents, random);
  }
}


public abstract class Crossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Cross((TGenotype, TGenotype) parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[parents.Count];
    Parallel.For(0, parents.Count, i => {
     offspring[i] = Cross(parents[i], random, encoding, problem);
    });
    return offspring;
  }
}

public abstract class Crossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract TGenotype Cross((TGenotype, TGenotype) parents, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding) {
    var offspring = new TGenotype[parents.Count];
    Parallel.For(0, parents.Count, i => {
      offspring[i] = Cross(parents[i], random, encoding);
    });
    return offspring;
  }
  
  IReadOnlyList<TGenotype> ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Cross(parents, random, encoding);
  }
}

public abstract class Crossover<TGenotype> : ICrossover<TGenotype>
{
  public abstract TGenotype Cross((TGenotype, TGenotype) parents, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random) {
    var offspring = new TGenotype[parents.Count];
    Parallel.For(0, parents.Count, i => {
      offspring[i] = Cross(parents[i], random);
    });
    return offspring;
  }
  
  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Cross(parents, random);
  }
  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Cross(parents, random);
  }
}


public class RandomCrossover<TGenotype> : BatchCrossover<TGenotype>
{
  public double Bias { get; }
  public RandomCrossover(double bias = 0.5) {
    if (bias is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(bias), "Bias must be between 0 and 1.");
    Bias = bias;
  }

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random) {
    double[] randoms = random.Random(parents.Count);
    var offspring = new TGenotype[parents.Count];
    
    Parallel.For(0, parents.Count, i => {
      var (parent1, parent2) = parents[i];
      offspring[i] = randoms[i] < Bias ? parent1 : parent2;
    });

    return offspring;
  }
}
  
public class MultiCrossover<TGenotype, TEncoding, TProblem> : BatchCrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public IReadOnlyList<ICrossover<TGenotype, TEncoding, TProblem>> Crossovers { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double sumWeights;
  private readonly double[] cumulativeSumWeights;
  
  public MultiCrossover(IReadOnlyList<ICrossover<TGenotype, TEncoding, TProblem>> crossovers, IReadOnlyList<double>? weights = null) {
    if (crossovers.Count == 0) throw new ArgumentException("At least one crossover must be provided.", nameof(crossovers));
    if (weights != null && weights.Count != crossovers.Count) throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
    if (weights != null && weights.Any(p => p < 0)) throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    if (weights != null && weights.All(p => p <= 0)) throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    
    Crossovers = crossovers;
    Weights = weights ?? Enumerable.Repeat(1.0, crossovers.Count).ToArray();
    
    cumulativeSumWeights = new double[Weights.Count];
    for (int i = 0; i < Weights.Count; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }
  
  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    int offspringCount = parents.Count;
    
    // determine which crossover to use for each offspring
    int[] operatorAssignment = new int[offspringCount];
    int[] operatorCounts = new int[Crossovers.Count];
    double[] randoms = random.Random(offspringCount);
    for (int i = 0; i < offspringCount; i++) {
      double r = randoms[i] * sumWeights;
      int idx = Array.FindIndex(cumulativeSumWeights, w => r < w);
      operatorAssignment[i] = idx;
      operatorCounts[idx]++;
    }
    
    // batch parents by operator
    var parentBatches = new List<(TGenotype, TGenotype)>[Crossovers.Count];
    for (int i = 0; i < Crossovers.Count; i++) {
      parentBatches[i] = new List<(TGenotype, TGenotype)>(operatorCounts[i]);
    }
    for (int i = 0; i < offspringCount; i++) {
      int opIdx = operatorAssignment[i];
      parentBatches[opIdx].Add(parents[i]);
    }
    
    // batch-create for each operator and collect
    var offspring = new List<TGenotype>(offspringCount);
    
    for (int i = 0; i < Crossovers.Count; i++) {
      var batchOffspring = Crossovers[i].Cross(parentBatches[i], random, encoding, problem);
      offspring.AddRange(batchOffspring);
    }
    
    return offspring;
  }
  
  
  // public override void Cross(ReadOnlySpan<TGenotype> parent1, ReadOnlySpan<TGenotype> parent2, IRandomNumberGenerator random, TEncoding encoding, TProblem problem, Span<TGenotype> offspring) {
  //   if (parent1.Length != parent2.Length || offspring.Length != parent1.Length) throw new ArgumentException("Parent arrays and offspring array must have the same length.", nameof(parent1));
  //
  //   // Compute cumulative weights for roulette wheel selection
  //   double[] cumulative = new double[Weights.Count];
  //   double sum = 0;
  //   for (int i = 0; i < Weights.Count; i++) {
  //     sum += Weights[i];
  //     cumulative[i] = sum;
  //   }
  //
  //   int n = offspring.Length;
  //   int[] operatorIndices = new int[n];
  //   for (int i = 0; i < n; i++) {
  //     double r = random.Random() * totalWeightSum;
  //     int idx = Array.FindIndex(cumulative, w => r < w);
  //     operatorIndices[i] = idx;
  //   }
  //
  //   // Batch indices by operator
  //   for (int op = 0; op < Crossovers.Count; op++) {
  //     // Find all indices for this operator
  //     var indices = Enumerable.Range(0, n).Where(i => operatorIndices[i] == op).ToArray();
  //
  //     if (indices.Length == 0) continue;
  //
  //     // Create temporary arrays for this batch
  //     TGenotype[] p1 = new TGenotype[indices.Length];
  //     TGenotype[] p2 = new TGenotype[indices.Length];
  //     for (int j = 0; j < indices.Length; j++) {
  //       p1[j] = parent1[indices[j]];
  //       p2[j] = parent2[indices[j]];
  //     }
  //     TGenotype[] off = new TGenotype[indices.Length];
  //
  //     Crossovers[op].Cross(p1, p2, random, encoding, problem, off);
  //
  //     for (int j = 0; j < indices.Length; j++) {
  //       offspring[indices[j]] = off[j];
  //     }
  //   }
  // }
}
