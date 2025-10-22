using HEAL.HeuristicLib.Operators.BatchOperators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype, in TEncoding> : IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);
}

public interface IMutator<TGenotype> : IMutator<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);
}

public abstract class BatchMutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class BatchMutator<TGenotype> : IMutator<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Mutate(parent, random);
  }
}

public abstract class Mutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, TProblem>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return parent.ParallelSelect(random, (_, x, r) => Mutate(x, r, encoding, problem));
  }
}

public abstract class Mutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding) {
    return parent.ParallelSelect(random, (_, x, r) => Mutate(x, r, encoding));
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    return parent.ParallelSelect(random, (_, x, r) => Mutate(x, r));
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Mutate(parent, random);
  }
}

// This would also work for other operators

// ToDo: extract base class for multi-operators 
public class MultiMutator<TGenotype, TEncoding, TProblem> : BatchMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> Mutators { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double sumWeights;
  private readonly double[] cumulativeSumWeights;

  public MultiMutator(IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> mutators, IReadOnlyList<double>? weights = null) {
    if (mutators.Count == 0) throw new ArgumentException("At least one crossover must be provided.", nameof(mutators));
    if (weights != null && weights.Count != mutators.Count) throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
    if (weights != null && weights.Any(p => p < 0)) throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    if (weights != null && weights.All(p => p <= 0)) throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));

    Mutators = mutators;
    Weights = weights ?? Enumerable.Repeat(1.0, mutators.Count).ToArray();

    cumulativeSumWeights = new double[Weights.Count];
    for (int i = 0; i < Weights.Count; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
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
      var batchOffspring = Mutators[i].Mutate(parentBatches[i], random, encoding, problem);
      offspring.AddRange(batchOffspring);
    }

    return offspring;
  }
}
