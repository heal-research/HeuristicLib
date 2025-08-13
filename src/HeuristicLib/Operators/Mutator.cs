using System.Diagnostics;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public interface IMutator<TGenotype, in TEncoding> : IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
{
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);
}

public interface IMutator<TGenotype> : IMutator<TGenotype, IEncoding<TGenotype>>
{
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);
}


public abstract class BatchMutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchMutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class BatchMutator<TGenotype> : IMutator<TGenotype>
{
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
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, TProblem>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[parent.Count];
    Parallel.For(0, parent.Count, i => {
      offspring[i] = Mutate(parent[i], random, encoding, problem);
    });
    return offspring;
  }
}

public abstract class Mutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding) {
    var offspring = new TGenotype[parent.Count];
    Parallel.For(0, parent.Count, i => {
      offspring[i] = Mutate(parent[i], random, encoding);
    });
    return offspring;
  }
  
  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    var offspring = new TGenotype[parent.Count];
    Parallel.For(0, parent.Count, i => {
      offspring[i] = Mutate(parent[i], random);
    });
    return offspring;
  }
  
  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }
  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Mutate(parent, random);
  }
}


public class NoChangeMutator<TGenotype> : BatchMutator<TGenotype>
{
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    return parent;
  }
}


// This would also work for other operators

public class MeasuredMutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  private readonly IMutator<TGenotype, TEncoding, TProblem> mutator;

  public OperatorMetric Metric { get; private set; }

  public MeasuredMutator(IMutator<TGenotype, TEncoding, TProblem> mutator) {
    this.mutator = mutator;

    Metric = new OperatorMetric();
  }

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    long start = Stopwatch.GetTimestamp();
    var offspring = mutator.Mutate(parent, random, encoding, problem);
    long end = Stopwatch.GetTimestamp();
    
    Metric += new OperatorMetric(offspring.Count, Stopwatch.GetElapsedTime(start, end)); 
    
    return offspring;
  }
}

public static class MeasuredMutatorExtension
{
  public static MeasuredMutator<TGenotype, TEncoding, TProblem> MeasureTime<TGenotype, TEncoding, TProblem>(this IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
  {
    return new MeasuredMutator<TGenotype, TEncoding, TProblem>(mutator);
  }
}


// ToDo: extract base class for multi-operators 
public class MultiMutator<TGenotype, TEncoding, TProblem> : BatchMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
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
