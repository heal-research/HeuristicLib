using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record WrappingMutator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected delegate IReadOnlyList<TCandidate> InnerMutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected IMutator<TCandidate, TSearchSpace, TProblem> InnerMutator { get; }

    protected WrappingMutator(IMutator<TCandidate, TSearchSpace, TProblem> innerMutator)
    {
        InnerMutator = innerMutator;
    }

    public IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
      => new Instance(this, instanceRegistry.Resolve(InnerMutator).Mutate, CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TExecutionState executionState, InnerMutate innerMutate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(WrappingMutator<TCandidate, TSearchSpace, TProblem, TExecutionState> wrappingMutator, InnerMutate innerMutate, TExecutionState executionState)
      : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
          => wrappingMutator.Mutate(parents, executionState, innerMutate, random, searchSpace, problem);
    }
}

public abstract record WrappingMutator<TCandidate, TSearchSpace, TProblem>
  : WrappingMutator<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingMutator(IMutator<TCandidate, TSearchSpace, TProblem> innerMutator)
      : base(innerMutator)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, NoState executionState,
      InnerMutate innerMutate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Mutate(parents, innerMutate, random, searchSpace, problem);

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, InnerMutate innerMutate,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
