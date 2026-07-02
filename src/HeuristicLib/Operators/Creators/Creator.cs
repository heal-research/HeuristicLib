using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record Creator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ICreator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new CreatorInstance(this, CreateInitialState());

    private sealed class CreatorInstance : ICreatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        private readonly Creator<TCandidate, TSearchSpace, TProblem, TExecutionState> creator;
        private readonly TExecutionState executionState;

        public CreatorInstance(Creator<TCandidate, TSearchSpace, TProblem, TExecutionState> creator, TExecutionState initialState)
        {
            this.creator = creator;
            executionState = initialState;
        }

        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return creator.Create(count, executionState, random, searchSpace, problem);
        }
    }
}

public abstract record Creator<TCandidate, TSearchSpace, TExecutionState>
  : ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new CreatorInstance(this, CreateInitialState());

    private sealed class CreatorInstance : ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        private readonly Creator<TCandidate, TSearchSpace, TExecutionState> creator;
        private readonly TExecutionState executionState;

        public CreatorInstance(Creator<TCandidate, TSearchSpace, TExecutionState> creator, TExecutionState initialState)
        {
            this.creator = creator;
            executionState = initialState;
        }

        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return creator.Create(count, executionState, random, searchSpace);
        }
    }
}


public abstract record Creator<TCandidate, TExecutionState>
  : ICreator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(int count, TExecutionState executionState, IRandomNumberGenerator random);

    public ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new CreatorInstance(this, CreateInitialState());

    private sealed class CreatorInstance : ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        private readonly Creator<TCandidate, TExecutionState> creator;
        private readonly TExecutionState executionState;

        public CreatorInstance(Creator<TCandidate, TExecutionState> creator, TExecutionState initialState)
        {
            this.creator = creator;
            executionState = initialState;
        }

        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return creator.Create(count, executionState, random);
        }
    }
}
