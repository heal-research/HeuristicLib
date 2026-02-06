using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

// ToDo: Think of a better name, maybe "ChooseOneCrossover".
public class MultiCrossover<TGenotype, TSearchSpace, TProblem> : Crossover<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReadOnlyList<ICrossover<TGenotype, TSearchSpace, TProblem>> Crossovers { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double sumWeights;
  private readonly double[] cumulativeSumWeights;

  public MultiCrossover(IReadOnlyList<ICrossover<TGenotype, TSearchSpace, TProblem>> crossovers, IReadOnlyList<double>? weights = null)
  {
    if (crossovers.Count == 0) {
      throw new ArgumentException("At least one crossover must be provided.", nameof(crossovers));
    }

    if (weights != null && weights.Count != crossovers.Count) {
      throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
    }

    if (weights != null && weights.Any(p => p < 0)) {
      throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    }

    if (weights != null && weights.All(p => p <= 0)) {
      throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    }

    Crossovers = crossovers;
    Weights = weights ?? Enumerable.Repeat(1.0, crossovers.Count).ToArray();

    cumulativeSumWeights = new double[Weights.Count];
    for (var i = 0; i < Weights.Count; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }
  
  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var crossoverInstances = Crossovers.Select(instanceRegistry.GetOrCreate).ToArray();
    return new Instance(crossoverInstances, cumulativeSumWeights, sumWeights);
  }

  public class Instance : CrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly IReadOnlyList<ICrossoverInstance<TGenotype, TSearchSpace, TProblem>> crossovers;
    private readonly double[] cumulativeSumWeights;
    private readonly double sumWeights;
    
    public Instance(IReadOnlyList<ICrossoverInstance<TGenotype, TSearchSpace, TProblem>> crossovers, double[] cumulativeSumWeights, double sumWeights)
    {
      this.crossovers = crossovers;
      this.cumulativeSumWeights = cumulativeSumWeights;
      this.sumWeights = sumWeights;
    }

    public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      var offspringCount = parents.Count;

      // determine which crossover to use for each offspring
      var operatorAssignment = new int[offspringCount];
      var operatorCounts = new int[crossovers.Count];
      var randoms = random.NextDoubles(offspringCount);
      for (var i = 0; i < offspringCount; i++) {
        var r = randoms[i] * sumWeights;
        var idx = Array.FindIndex(cumulativeSumWeights, w => r < w);
        operatorAssignment[i] = idx;
        operatorCounts[idx]++;
      }

      // batch parents by operator
      var parentBatches = new List<IParents<TGenotype>>[crossovers.Count];
      for (var i = 0; i < crossovers.Count; i++) {
        parentBatches[i] = new List<IParents<TGenotype>>(operatorCounts[i]);
      }

      for (var i = 0; i < offspringCount; i++) {
        var opIdx = operatorAssignment[i];
        parentBatches[opIdx].Add(parents[i]);
      }

      // batch-create for each operator and collect
      var offspring = new List<TGenotype>(offspringCount);

      for (var i = 0; i < crossovers.Count; i++) {
        var batchOffspring = crossovers[i].Cross(parentBatches[i], random, searchSpace, problem);
        offspring.AddRange(batchOffspring);
      }

      return offspring;
    }

    // public override void Cross(ReadOnlySpan<TGenotype> parent1, ReadOnlySpan<TGenotype> parent2, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem, Span<TGenotype> offspring) {
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
    //     // GetEvaluator temporary arrays for this batch
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
}

// public static class MultiCrossover
// {
//   public static ICrossover<TGenotype, TSearchSpace, TProblem> WithProbability<TGenotype, TSearchSpace, TProblem>(this ICrossover<TGenotype, TSearchSpace, TProblem> crossover, double probability) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => new MultiCrossover<TGenotype, TSearchSpace, TProblem>([crossover, NoCrossover<TGenotype>.Instance], [probability, 1 - probability]);
// }
