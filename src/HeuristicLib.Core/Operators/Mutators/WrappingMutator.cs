using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record WrappingMutator<TG, TS, TP, TExecutionState>
  : IMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TExecutionState : class
{
  protected delegate IReadOnlyList<TG> InnerMutate(IReadOnlyList<TG> parents, IRandomNumberGenerator random, TS searchSpace, TP problem);

  protected IMutator<TG, TS, TP> InnerMutator { get; }

  protected WrappingMutator(IMutator<TG, TS, TP> innerMutator)
  {
    InnerMutator = innerMutator;
  }

  public IMutatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance(this, instanceRegistry.Resolve(InnerMutator).Mutate, CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, TExecutionState executionState, InnerMutate innerMutate, IRandomNumberGenerator random, TS searchSpace, TP problem);

  private sealed class Instance(WrappingMutator<TG, TS, TP, TExecutionState> wrappingMutator, InnerMutate innerMutate, TExecutionState executionState)
    : IMutatorInstance<TG, TS, TP>
  {
    public IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
      => wrappingMutator.Mutate(parents, executionState, innerMutate, random, searchSpace, problem);
  }
}

public abstract record WrappingMutator<TG, TS, TP>
  : WrappingMutator<TG, TS, TP, NoState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  protected WrappingMutator(IMutator<TG, TS, TP> innerMutator)
    : base(innerMutator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, NoState executionState,
    InnerMutate innerMutate, IRandomNumberGenerator random, TS searchSpace, TP problem)
    => Mutate(parents, innerMutate, random, searchSpace, problem);

  protected abstract IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, InnerMutate innerMutate,
    IRandomNumberGenerator random, TS searchSpace, TP problem);
}
