using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatefulTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
  where TAlgorithmState : class, IAlgorithmState
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState, TState terminatorState, TSearchSpace searchSpace, TProblem problem);

  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return terminator.ShouldTerminate(state, terminatorState, searchSpace, problem);
    }
  }
}

public abstract record StatefulTerminator<TGenotype, TSearchSpace, TState, TAlgorithmState>
  : ITerminator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState state, TState terminatorState, TSearchSpace searchSpace);

  public ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TSearchSpace, TState, TAlgorithmState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return terminator.ShouldTerminate(state, terminatorState, searchSpace);
    }
  }
}

public abstract record StatefulTerminator<TGenotype, TAlgorithmState, TState>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState state, TState terminatorState);

  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TAlgorithmState, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  {
    public bool ShouldTerminate(TAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return terminator.ShouldTerminate(state, terminatorState);
    }
  }
}

public abstract record StatefulTerminator<TGenotype, TState>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TState terminatorState);

  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>
  {
    public bool ShouldTerminate(IAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return terminator.ShouldTerminate(terminatorState);
    }
  }
}

