using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record PipelineMutator<TG, TS, TP> 
  : StatefulMutator<TG, TS, TP, PipelineMutator<TG, TS, TP>.State>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality] public ImmutableArray<IMutator<TG, TS, TP>> Mutators { get; }

  public PipelineMutator(ImmutableArray<IMutator<TG, TS, TP>> mutators)
  {
    // ToDo: think if we want to allow empty pipelines.
    if (mutators.Length == 0) {
      throw new ArgumentException("At least one crossover must be provided.", nameof(mutators));
    }

    Mutators = mutators;
  }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) 
    => new (Mutators.Select(instanceRegistry.Resolve).ToArray());
  
  protected override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, State state, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var current = parents;
    foreach (var mutator in state.MutatorInstances) {
      current = mutator.Mutate(current, random, searchSpace, problem);
    }
    return current;
  }

  public sealed class State(IReadOnlyList<IMutatorInstance<TG, TS, TP>> mutatorInstances)
  {
    public IReadOnlyList<IMutatorInstance<TG, TS, TP>> MutatorInstances { get; } = mutatorInstances;
  }
}
