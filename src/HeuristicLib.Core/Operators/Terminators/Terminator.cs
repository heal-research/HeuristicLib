using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record Terminator<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
  where TSearchState : class, ISearchState
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract bool ShouldTerminate(TSearchState searchState, TExecutionState executionState, TSearchSpace searchSpace, TProblem problem);

  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new TerminatorInstance(this, CreateInitialState());

  private sealed class TerminatorInstance(Terminator<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState> terminator, TExecutionState executionState)
    : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public bool ShouldTerminate(TSearchState state, TSearchSpace searchSpace, TProblem problem)
    {
      return terminator.ShouldTerminate(state, executionState, searchSpace, problem);
    }
  }
}

public abstract record Terminator<TGenotype, TSearchSpace, TExecutionState, TSearchState>
  : ITerminator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract bool ShouldTerminate(TSearchState state, TExecutionState executionState, TSearchSpace searchSpace);

  public ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new TerminatorInstance(this, CreateInitialState());

  private sealed class TerminatorInstance(Terminator<TGenotype, TSearchSpace, TExecutionState, TSearchState> terminator, TExecutionState executionState)
    : ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>
  {
    public bool ShouldTerminate(TSearchState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return terminator.ShouldTerminate(state, executionState, searchSpace);
    }
  }
}

public abstract record Terminator<TGenotype, TSearchState, TExecutionState>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>
  where TSearchState : class, ISearchState
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract bool ShouldTerminate(TSearchState state, TExecutionState executionState);

  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new TerminatorInstance(this, CreateInitialState());

  private sealed class TerminatorInstance(Terminator<TGenotype, TSearchState, TExecutionState> terminator, TExecutionState executionState)
    : ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>
  {
    public bool ShouldTerminate(TSearchState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return terminator.ShouldTerminate(state, executionState);
    }
  }
}

public abstract record Terminator<TGenotype, TExecutionState>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract bool ShouldTerminate(TExecutionState executionState);

  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new TerminatorInstance(this, CreateInitialState());

  private sealed class TerminatorInstance(Terminator<TGenotype, TExecutionState> terminator, TExecutionState executionState)
    : ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState>
  {
    public bool ShouldTerminate(ISearchState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return terminator.ShouldTerminate(executionState);
    }
  }
}

