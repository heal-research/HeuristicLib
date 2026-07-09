using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public abstract partial record MultiMutator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    // ToDo: is this really an expressive name?
    [OrderedEquality] protected ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> InnerMutators { get; }

    protected delegate IReadOnlyList<TCandidate> InnerMutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected MultiMutator(ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> innerMutators)
    {
        InnerMutators = innerMutators;
    }

    public IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
      => new Instance(this, InnerMutators.Select(instanceRegistry.Resolve).Select(x => (InnerMutate)x.Mutate).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TExecutionState executionState, IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiMutator<TCandidate, TSearchSpace, TProblem, TExecutionState> multiMutator, IReadOnlyList<InnerMutate> innerMutators, TExecutionState executionState)
      : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
          => multiMutator.Mutate(parents, executionState, innerMutators, random, searchSpace, problem);
    }
}

public abstract record MultiMutator<TCandidate, TSearchSpace, TProblem>
  : MultiMutator<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiMutator(ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> innerMutators)
      : base(innerMutators)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, NoState executionState,
      IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Mutate(parents, innerMutators, random, searchSpace, problem);

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents,
      IReadOnlyList<InnerMutate> innerMutators, IRandomNumberGenerator random, TSearchSpace searchSpace,
      TProblem problem);
}
