using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record Terminator<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
  where TSearchState : class, ISearchState
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TExecutionState executionState, TSearchSpace searchSpace, TProblem problem);

    public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new TerminatorInstance(this, CreateInitialState());

    private sealed class TerminatorInstance(Terminator<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState> terminator, TExecutionState executionState)
      : ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            return terminator.IsTerminalState(state, executionState, searchSpace, problem);
        }
    }
}

public abstract record Terminator<TCandidate, TSearchSpace, TExecutionState, TSearchState>
  : ITerminator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState state, TExecutionState executionState, TSearchSpace searchSpace);

    public ITerminatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new TerminatorInstance(this, CreateInitialState());

    private sealed class TerminatorInstance(Terminator<TCandidate, TSearchSpace, TExecutionState, TSearchState> terminator, TExecutionState executionState)
      : ITerminatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    {
        public bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return terminator.IsTerminalState(state, executionState, searchSpace);
        }
    }
}

public abstract record Terminator<TCandidate, TSearchState, TExecutionState>
  : ITerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
  where TSearchState : class, ISearchState
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState state, TExecutionState executionState);

    public ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new TerminatorInstance(this, CreateInitialState());

    private sealed class TerminatorInstance(Terminator<TCandidate, TSearchState, TExecutionState> terminator, TExecutionState executionState)
      : ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    {
        public bool IsTerminalState(TSearchState state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return terminator.IsTerminalState(state, executionState);
        }
    }
}

public abstract record Terminator<TCandidate, TExecutionState>
  : ITerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TExecutionState executionState);

    public ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new TerminatorInstance(this, CreateInitialState());

    private sealed class TerminatorInstance(Terminator<TCandidate, TExecutionState> terminator, TExecutionState executionState)
      : ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>
    {
        public bool IsTerminalState(ISearchState state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return terminator.IsTerminalState(executionState);
        }
    }
}

