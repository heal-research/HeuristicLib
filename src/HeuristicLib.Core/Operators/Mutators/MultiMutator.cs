using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public abstract partial record MultiMutator<TG, TS, TP, TExecutionState>
  : IMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  // ToDo: is this really an expressive name?
  [OrderedEquality] protected ImmutableArray<IMutator<TG, TS, TP>> InnerMutators { get; }

  protected delegate IReadOnlyList<TG> InnerMutate(IReadOnlyList<TG> parents, IRandomNumberGenerator random, TS searchSpace, TP problem);

  protected MultiMutator(ImmutableArray<IMutator<TG, TS, TP>> innerMutators)
  {
    InnerMutators = innerMutators;
  }

  public IMutatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance(this, InnerMutators.Select(instanceRegistry.Resolve).Select(x => (InnerMutate)x.Mutate).ToArray(), CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, TExecutionState executionState, IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TS searchSpace, TP problem);

  private sealed class Instance(MultiMutator<TG, TS, TP, TExecutionState> multiMutator, IReadOnlyList<InnerMutate> innerMutators, TExecutionState executionState)
    : IMutatorInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
      => multiMutator.Mutate(parents, executionState, innerMutators, random, searchSpace, problem);
  }
}

public abstract record MultiMutator<TG, TS, TP>
  : MultiMutator<TG, TS, TP, NoState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  protected MultiMutator(ImmutableArray<IMutator<TG, TS, TP>> innerMutators)
    : base(innerMutators)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, NoState executionState,
    IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TS searchSpace, TP problem)
    => Mutate(parents, innerMutators, random, searchSpace, problem);

  protected abstract IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents,
    IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TS searchSpace,
    TP problem);
}
