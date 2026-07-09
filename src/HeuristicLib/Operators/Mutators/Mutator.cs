using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record Mutator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new MutatorInstance(this, CreateInitialState());

    private sealed class MutatorInstance(Mutator<TCandidate, TSearchSpace, TProblem, TExecutionState> mutator, TExecutionState executionState)
      : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return mutator.Mutate(parents, executionState, random, searchSpace, problem);
        }
    }
}

public abstract record Mutator<TCandidate, TSearchSpace, TExecutionState>
  : IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace);

    public IMutatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new MutatorInstance(this, CreateInitialState());

    private sealed class MutatorInstance : IMutatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        private readonly Mutator<TCandidate, TSearchSpace, TExecutionState> mutator;
        private readonly TExecutionState executionState;

        public MutatorInstance(Mutator<TCandidate, TSearchSpace, TExecutionState> mutator, TExecutionState initialState)
        {
            this.mutator = mutator;
            executionState = initialState;
        }

        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return mutator.Mutate(parents, executionState, random, searchSpace);
        }
    }
}

public abstract record Mutator<TCandidate, TExecutionState>
  : IMutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TExecutionState executionState,
      IRandomNumberGenerator random);

    public IMutatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new MutatorInstance(this, CreateInitialState());

    private sealed class MutatorInstance : IMutatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        private readonly Mutator<TCandidate, TExecutionState> mutator;
        private readonly TExecutionState executionState;

        public MutatorInstance(Mutator<TCandidate, TExecutionState> mutator, TExecutionState initialState)
        {
            this.mutator = mutator;
            executionState = initialState;
        }

        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return mutator.Mutate(parents, executionState, random);
        }
    }
}
