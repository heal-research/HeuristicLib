using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator;

public class MultiMutator<TGenotype, TEncoding, TProblem> : BatchMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> Mutators { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double sumWeights;
  private readonly double[] cumulativeSumWeights;

  public MultiMutator(IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> mutators, IReadOnlyList<double>? weights = null) {
    if (mutators.Count == 0)
      throw new ArgumentException("At least one crossover must be provided.", nameof(mutators));
    if (weights != null && weights.Count != mutators.Count)
      throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
    if (weights != null && weights.Any(p => p < 0))
      throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    if (weights != null && weights.All(p => p <= 0))
      throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));

    Mutators = mutators;
    Weights = weights ?? Enumerable.Repeat(1.0, mutators.Count).ToArray();

    cumulativeSumWeights = new double[Weights.Count];
    for (var i = 0; i < Weights.Count; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspringCount = parent.Count;

    // determine which crossover to use for each offspring
    var operatorAssignment = new int[offspringCount];
    var operatorCounts = new int[Mutators.Count];
    var randoms = random.Random(offspringCount);
    for (var i = 0; i < offspringCount; i++) {
      var r = randoms[i] * sumWeights;
      var idx = Array.FindIndex(cumulativeSumWeights, w => r < w);
      operatorAssignment[i] = idx;
      operatorCounts[idx]++;
    }

    // batch parents by operator
    var parentBatches = new List<TGenotype>[Mutators.Count];
    for (var i = 0; i < Mutators.Count; i++) {
      parentBatches[i] = new List<TGenotype>(operatorCounts[i]);
    }

    for (var i = 0; i < offspringCount; i++) {
      var opIdx = operatorAssignment[i];
      parentBatches[opIdx].Add(parent[i]);
    }

    // batch-create for each operator and collect
    var offspring = new List<TGenotype>(offspringCount);

    for (var i = 0; i < Mutators.Count; i++) {
      var batchOffspring = Mutators[i].Mutate(parentBatches[i], random, encoding, problem);
      offspring.AddRange(batchOffspring);
    }

    return offspring;
  }
}

public class MultiMutator<TGenotype, TEncoding> : MultiMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>, IMutator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public MultiMutator(IReadOnlyList<IMutator<TGenotype, TEncoding>> mutators, IReadOnlyList<double>? weights = null) : base(mutators, weights) { }
  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding) => base.Mutate(parent, random, encoding, null!);
}

public class MultiMutator<TGenotype> : MultiMutator<TGenotype, IEncoding<TGenotype>>, IMutator<TGenotype> {
  public MultiMutator(IReadOnlyList<IMutator<TGenotype>> mutators, IReadOnlyList<double>? weights = null) : base(mutators, weights) { }
  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) => base.Mutate(parent, random, null!, null!);
}

public static class MultiMutator {
  public static MultiMutator<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(
    IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> mutators, IReadOnlyList<double>? weights = null)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MultiMutator<TGenotype, TEncoding, TProblem>(mutators, weights);
  }

  public static MultiMutator<TGenotype, TEncoding> Create<TGenotype, TEncoding>(
    IReadOnlyList<IMutator<TGenotype, TEncoding>> mutators, IReadOnlyList<double>? weights = null)
    where TEncoding : class, IEncoding<TGenotype> {
    return new MultiMutator<TGenotype, TEncoding>(mutators, weights);
  }

  public static MultiMutator<TGenotype> Create<TGenotype>(
    IReadOnlyList<IMutator<TGenotype>> mutators, IReadOnlyList<double>? weights = null) {
    return new MultiMutator<TGenotype>(mutators, weights);
  }

  public static MultiMutator<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(
    params IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> mutators)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MultiMutator<TGenotype, TEncoding, TProblem>(mutators);
  }

  public static MultiMutator<TGenotype, TEncoding> Create<TGenotype, TEncoding>(params IReadOnlyList<IMutator<TGenotype, TEncoding>> mutators)
    where TEncoding : class, IEncoding<TGenotype> {
    return new MultiMutator<TGenotype, TEncoding>(mutators);
  }

  public static MultiMutator<TGenotype> Create<TGenotype>(params IReadOnlyList<IMutator<TGenotype>> mutators) {
    return new MultiMutator<TGenotype>(mutators);
  }

  public static MultiMutator<TGenotype, TEncoding, TProblem> WithRate<TGenotype, TEncoding, TProblem>(this IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return Create([mutator, NoChangeMutator<TGenotype>.Instance], [mutationRate, 1 - mutationRate]);
  }
}
