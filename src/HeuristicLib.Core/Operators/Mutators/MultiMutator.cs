using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

// ToDo: Think of a better name, maybe "ChooseOneMutator".
[Equatable]
public partial record MultiMutator<TGenotype, TSearchSpace, TProblem>
  : StatefulMutator<TGenotype, TSearchSpace, TProblem, MultiMutator<TGenotype, TSearchSpace, TProblem>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality]
  public ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> Mutators { get; }

  [OrderedEquality]
  public ImmutableArray<double> Weights { get; }

  [IgnoreEquality]
  private readonly double sumWeights;

  [IgnoreEquality]
  private readonly double[] cumulativeSumWeights;

  public MultiMutator(ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
  {
    if (mutators.Length == 0) {
      throw new ArgumentException("At least one mutator must be provided.", nameof(mutators));
    }

    if (weights is not null && weights.Value.Length != mutators.Length) {
      throw new ArgumentException("Weights must have the same length as mutator.", nameof(weights));
    }

    if (weights is not null && weights.Value.Any(p => p < 0)) {
      throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    }

    if (weights is not null && weights.Value.All(p => p <= 0)) {
      throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    }

    Mutators = mutators;
    Weights = weights ?? [.. Enumerable.Repeat(1.0, mutators.Length)];

    cumulativeSumWeights = new double[Weights.Length];
    for (var i = 0; i < Weights.Length; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry)
  {
    var mutatorInstances = Mutators.Select(instanceRegistry.Resolve).ToArray();
    return new State(mutatorInstances, cumulativeSumWeights, sumWeights);
  }

  protected override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents, State state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
  {
    // ToDo: Unify the logic for MultiOperators to avoid duplication.

    var offspringCount = parents.Count;
    var mutators = state.MutatorInstances;

    // determine which mutator to use for each offspring
    var operatorAssignment = new int[offspringCount];
    var operatorCounts = new int[mutators.Count];
    var randoms = random.NextDoubles(offspringCount);
    for (var i = 0; i < offspringCount; i++) {
      var r = randoms[i] * sumWeights;
      var idx = Array.FindIndex(cumulativeSumWeights, w => r < w);
      operatorAssignment[i] = idx;
      operatorCounts[idx]++;
    }

    // batch parent by operator
    var parentBatches = new List<TGenotype>[mutators.Count];
    for (var i = 0; i < mutators.Count; i++) {
      parentBatches[i] = new List<TGenotype>(operatorCounts[i]);
    }

    for (var i = 0; i < offspringCount; i++) {
      var opIdx = operatorAssignment[i];
      parentBatches[opIdx].Add(parents[i]);
    }

    // batch-create for each operator and collect
    var offspring = new List<TGenotype>(offspringCount);

    for (var i = 0; i < mutators.Count; i++) {
      var batchOffspring = mutators[i].Mutate(parentBatches[i], random, searchSpace, problem);
      offspring.AddRange(batchOffspring);
    }

    return offspring;
  }
  
  // ToDo: probably this can be in a base class on ensemble operators
  public sealed class State(IReadOnlyList<IMutatorInstance<TGenotype, TSearchSpace, TProblem>> mutatorInstances, IReadOnlyList<double> cumulativeSumWeights, double sumWeights)
  {
    public IReadOnlyList<IMutatorInstance<TGenotype, TSearchSpace, TProblem>> MutatorInstances { get; } = mutatorInstances;
    public IReadOnlyList<double> CumulativeSumWeights { get; } = cumulativeSumWeights;
    public double SumWeights { get; } = sumWeights;
  }
}

public static class MultiMutator
{
  public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(params IEnumerable<IMutator<TGenotype, TSearchSpace, TProblem>> mutators)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    var r = mutators.ToImmutableArray();
    var weights = r.Select(_ => 1.0 / r.Length).ToImmutableArray();
    return new MultiMutator<TGenotype, TSearchSpace, TProblem>(r, weights);
  }
  public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    if (weights == null) {
      throw new ArgumentNullException(nameof(weights));
    }

    return new MultiMutator<TGenotype, TSearchSpace, TProblem>(mutators, weights);
  }

  // public static MultiMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(IReadOnlyList<IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>> mutators, IReadOnlyList<double>? weights = null) 
  //   where TSearchSpace : class, ISearchSpace<TGenotype>
  // {
  //   return new(mutators, weights);
  // }

  // public static MultiMutator<TGenotype> Create<TGenotype>(IReadOnlyList<IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>> mutators, IReadOnlyList<double>? weights = null)
  // {
  //   return new(mutators, weights);
  // }

  // public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(params ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutators) 
  //   where TSearchSpace : class, ISearchSpace<TGenotype> 
  //   where TProblem : class, IProblem<TGenotype, TSearchSpace>
  // {
  //   return new MultiMutator<TGenotype, TSearchSpace, TProblem>(mutators);
  // }

  // public static MultiMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(params IReadOnlyList<IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>> mutators) 
  //   where TSearchSpace : class, ISearchSpace<TGenotype>
  // {
  //   return new(mutators);
  // }

  // public static MultiMutator<TGenotype> Create<TGenotype>(params IReadOnlyList<IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>> mutators)
  // {
  //   return new(mutators);
  // }

  extension<TGenotype, TSearchSpace, TProblem>(IMutator<TGenotype, TSearchSpace, TProblem> mutator)
   where TSearchSpace : class, ISearchSpace<TGenotype>
   where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    public MultiMutator<TGenotype, TSearchSpace, TProblem> WithRate(double mutationRate)
    {
      return Create([mutator, NoChangeMutator<TGenotype>.Instance], [mutationRate, 1 - mutationRate]);
    }
  }
}
