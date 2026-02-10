using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record class PipelineMutator<TG, TS, TP> : Mutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality]
  public ImmutableArray<IMutator<TG, TS, TP>> Mutators { get; }

  public PipelineMutator(ImmutableArray<IMutator<TG, TS, TP>> mutators)
  {
    if (mutators.Length == 0) {
      throw new ArgumentException("At least one crossover must be provided.", nameof(mutators));
    }

    this.Mutators = mutators;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var mutatorInstances = Mutators.Select(instanceRegistry.GetOrCreate).ToArray();
    return new Instance(mutatorInstances);
  }

  public class Instance : MutatorInstance<TG, TS, TP>
  {
    private readonly IReadOnlyList<IMutatorInstance<TG, TS, TP>> mutators;

    public Instance(IReadOnlyList<IMutatorInstance<TG, TS, TP>> mutators)
    {
      this.mutators = mutators;
    }

    public override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var current = parent;
      foreach (var mutator in mutators) {
        current = mutator.Mutate(current, random, searchSpace, problem);
      }

      return current;
    }
  }
}
