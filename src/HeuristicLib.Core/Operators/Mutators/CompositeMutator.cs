using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public abstract partial record CompositeMutator<TG, TS, TP, TState> 
  : IMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: is this really an expressive name?
  [OrderedEquality] protected ImmutableArray<IMutator<TG, TS, TP>> InnerMutators { get; }
  
  protected CompositeMutator(ImmutableArray<IMutator<TG, TS, TP>> innerMutators)
  {
    InnerMutators = innerMutators;
  }

  public IMutatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance(this, InnerMutators.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, TState state, IReadOnlyList<IMutatorInstance<TG, TS, TP>> innerMutators, IRandomNumberGenerator random, TS searchSpace, TP problem);
  
  private sealed class Instance(CompositeMutator<TG, TS, TP, TState> compositeMutator, IReadOnlyList<IMutatorInstance<TG, TS, TP>> innerMutators, TState state)
    : IMutatorInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, IRandomNumberGenerator random, TS searchSpace, TP problem) 
      => compositeMutator.Mutate(parents, state, innerMutators, random, searchSpace, problem);
  }
}
