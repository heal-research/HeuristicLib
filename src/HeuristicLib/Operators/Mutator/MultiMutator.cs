using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutator;

public class MultiMutator<TGenotype, TSearchSpace, TProblem> : BatchMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> Mutators { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double sumWeights;
  private readonly double[] cumulativeSumWeights;

  public MultiMutator(IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, IReadOnlyList<double>? weights = null) {
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
    for (int i = 0; i < Weights.Count; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    int offspringCount = parent.Count;

    // determine which crossover to use for each offspring
    int[] operatorAssignment = new int[offspringCount];
    int[] operatorCounts = new int[Mutators.Count];
    double[] randoms = random.Random(offspringCount);
    for (int i = 0; i < offspringCount; i++) {
      double r = randoms[i] * sumWeights;
      int idx = Array.FindIndex(cumulativeSumWeights, w => r < w);
      operatorAssignment[i] = idx;
      operatorCounts[idx]++;
    }

    // batch parents by operator
    var parentBatches = new List<TGenotype>[Mutators.Count];
    for (int i = 0; i < Mutators.Count; i++) {
      parentBatches[i] = new List<TGenotype>(operatorCounts[i]);
    }

    for (int i = 0; i < offspringCount; i++) {
      int opIdx = operatorAssignment[i];
      parentBatches[opIdx].Add(parent[i]);
    }

    // batch-create for each operator and collect
    var offspring = new List<TGenotype>(offspringCount);

    for (int i = 0; i < Mutators.Count; i++) {
      var batchOffspring = Mutators[i].Mutate(parentBatches[i], random, searchSpace, problem);
      offspring.AddRange(batchOffspring);
    }

    return offspring;
  }
}

public class MultiMutator<TGenotype, TSearchSpace> : MultiMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>, IMutator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public MultiMutator(IReadOnlyList<IMutator<TGenotype, TSearchSpace>> mutators, IReadOnlyList<double>? weights = null) : base(mutators, weights) { }
  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace) => base.Mutate(parent, random, searchSpace, null!);
}

public class MultiMutator<TGenotype> : MultiMutator<TGenotype, ISearchSpace<TGenotype>>, IMutator<TGenotype> {
  public MultiMutator(IReadOnlyList<IMutator<TGenotype>> mutators, IReadOnlyList<double>? weights = null) : base(mutators, weights) { }
  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) => base.Mutate(parent, random, null!, null!);
}

public static class MultiMutator {
  public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(
    IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, IReadOnlyList<double>? weights = null)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> {
    return new MultiMutator<TGenotype, TSearchSpace, TProblem>(mutators, weights);
  }

  public static MultiMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(
    IReadOnlyList<IMutator<TGenotype, TSearchSpace>> mutators, IReadOnlyList<double>? weights = null)
    where TSearchSpace : class, ISearchSpace<TGenotype> {
    return new MultiMutator<TGenotype, TSearchSpace>(mutators, weights);
  }

  public static MultiMutator<TGenotype> Create<TGenotype>(
    IReadOnlyList<IMutator<TGenotype>> mutators, IReadOnlyList<double>? weights = null) {
    return new MultiMutator<TGenotype>(mutators, weights);
  }

  public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(
    params IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> mutators)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> {
    return new MultiMutator<TGenotype, TSearchSpace, TProblem>(mutators);
  }

  public static MultiMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(params IReadOnlyList<IMutator<TGenotype, TSearchSpace>> mutators)
    where TSearchSpace : class, ISearchSpace<TGenotype> {
    return new MultiMutator<TGenotype, TSearchSpace>(mutators);
  }

  public static MultiMutator<TGenotype> Create<TGenotype>(params IReadOnlyList<IMutator<TGenotype>> mutators) {
    return new MultiMutator<TGenotype>(mutators);
  }

  public static MultiMutator<TGenotype, TSearchSpace, TProblem> WithRate<TGenotype, TSearchSpace, TProblem>(this IMutator<TGenotype, TSearchSpace, TProblem> mutator, double mutationRate) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> {
    return Create([mutator, NoChangeMutator<TGenotype>.Instance], [mutationRate, 1 - mutationRate]);
  }
}
