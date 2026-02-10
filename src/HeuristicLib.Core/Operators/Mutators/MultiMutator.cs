using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

// ToDo: Think of a better name, maybe "ChooseOneMutator".
[Equatable]
public partial record class MultiMutator<TGenotype, TSearchSpace, TProblem>
  : Mutator<TGenotype, TSearchSpace, TProblem>
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

  public MultiMutator(ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutator, ImmutableArray<double>? weights = null)
  {
    if (mutator.Length == 0) {
      throw new ArgumentException("At least one mutator must be provided.", nameof(mutator));
    }

    if (weights is not null && weights.Value.Length != mutator.Length) {
      throw new ArgumentException("Weights must have the same length as mutator.", nameof(weights));
    }

    if (weights is not null && weights.Value.Any(p => p < 0)) {
      throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    }

    if (weights is not null && weights.Value.All(p => p <= 0)) {
      throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    }

    Mutators = mutator;
    Weights = weights ?? [..Enumerable.Repeat(1.0, mutator.Length)];

    cumulativeSumWeights = new double[Weights.Length];
    for (var i = 0; i < Weights.Length; i++) {
      sumWeights += Weights[i];
      cumulativeSumWeights[i] = sumWeights;
    }
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var mutatorInstances = Mutators.Select(instanceRegistry.GetOrCreate).ToArray();
    return new Instance(mutatorInstances, cumulativeSumWeights, sumWeights);
  }

  public class Instance : MutatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly IReadOnlyList<IMutatorInstance<TGenotype, TSearchSpace, TProblem>> mutator;
    private readonly double[] cumulativeSumWeights;
    private readonly double sumWeights;

    public Instance(IReadOnlyList<IMutatorInstance<TGenotype, TSearchSpace, TProblem>> mutator, double[] cumulativeSumWeights, double sumWeights)
    {
      this.mutator = mutator;
      this.cumulativeSumWeights = cumulativeSumWeights;
      this.sumWeights = sumWeights;
    }

    public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      // ToDo: Unify the logic for MultiOperators to avoid duplication.

      var offspringCount = parent.Count;

      // determine which mutator to use for each offspring
      var operatorAssignment = new int[offspringCount];
      var operatorCounts = new int[mutator.Count];
      var randoms = random.NextDoubles(offspringCount);
      for (var i = 0; i < offspringCount; i++) {
        var r = randoms[i] * sumWeights;
        var idx = Array.FindIndex(cumulativeSumWeights, w => r < w);
        operatorAssignment[i] = idx;
        operatorCounts[idx]++;
      }

      // batch parent by operator
      var parentBatches = new List<TGenotype>[mutator.Count];
      for (var i = 0; i < mutator.Count; i++) {
        parentBatches[i] = new List<TGenotype>(operatorCounts[i]);
      }

      for (var i = 0; i < offspringCount; i++) {
        var opIdx = operatorAssignment[i];
        parentBatches[opIdx].Add(parent[i]);
      }

      // batch-create for each operator and collect
      var offspring = new List<TGenotype>(offspringCount);

      for (var i = 0; i < mutator.Count; i++) {
        var batchOffspring = mutator[i].Mutate(parentBatches[i], random, searchSpace, problem);
        offspring.AddRange(batchOffspring);
      }

      return offspring;
    }
  }
}

public static class MultiMutator
{
  public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    if (weights == null) {
      throw new ArgumentNullException(nameof(weights));
    }

    return new MultiMutator<TGenotype, TSearchSpace, TProblem>(mutators, weights);
  }
  
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
