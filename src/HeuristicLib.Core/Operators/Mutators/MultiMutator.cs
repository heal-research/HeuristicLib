using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

// ToDo: Think of a better name, maybe "ChooseOneMutator".
public class MultiMutator<TGenotype, TSearchSpace, TProblem> : Mutator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> Mutators { get; }
  public IReadOnlyList<double> Weights { get; }
  private readonly double sumWeights;
  private readonly double[] cumulativeSumWeights;

  public MultiMutator(IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> mutator, IReadOnlyList<double>? weights = null)
  {
    if (mutator.Count == 0) {
      throw new ArgumentException("At least one mutator must be provided.", nameof(mutator));
    }

    if (weights != null && weights.Count != mutator.Count) {
      throw new ArgumentException("Weights must have the same length as mutator.", nameof(weights));
    }

    if (weights != null && weights.Any(p => p < 0)) {
      throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    }

    if (weights != null && weights.All(p => p <= 0)) {
      throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    }

    Mutators = mutator;
    Weights = weights ?? Enumerable.Repeat(1.0, mutator.Count).ToArray();

    cumulativeSumWeights = new double[Weights.Count];
    for (var i = 0; i < Weights.Count; i++) {
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

// public static class MultiMutator
// {
//   public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, IReadOnlyList<double>? weights = null) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => new(mutators, weights);
//
//   public static MultiMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(IReadOnlyList<IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>> mutators, IReadOnlyList<double>? weights = null) where TSearchSpace : class, ISearchSpace<TGenotype> => new(mutators, weights);
//
//   public static MultiMutator<TGenotype> Create<TGenotype>(IReadOnlyList<IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>> mutators, IReadOnlyList<double>? weights = null) => new(mutators, weights);
//
//   public static MultiMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(params IReadOnlyList<IMutator<TGenotype, TSearchSpace, TProblem>> mutators) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => new(mutators);
//
//   public static MultiMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(params IReadOnlyList<IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>> mutators) where TSearchSpace : class, ISearchSpace<TGenotype> => new(mutators);
//
//   public static MultiMutator<TGenotype> Create<TGenotype>(params IReadOnlyList<IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>> mutators) => new(mutators);
//
//   public static MultiMutator<TGenotype, TSearchSpace, TProblem> WithRate<TGenotype, TSearchSpace, TProblem>(this IMutator<TGenotype, TSearchSpace, TProblem> mutator, double mutationRate) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => Create([mutator, NoChangeMutator<TGenotype>.Instance], [mutationRate, 1 - mutationRate]);
// }
