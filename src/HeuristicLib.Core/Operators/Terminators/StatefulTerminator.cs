using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatefulTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState>
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState, TState terminatorState, TSearchSpace searchSpace, TProblem problem);

  public ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return terminator.ShouldTerminate(state, terminatorState, searchSpace, problem);
    }
  }
}

public abstract record StatefulTerminator<TGenotype, TAlgorithmState, TSearchSpace, TState>
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState state, TState terminatorState, TSearchSpace searchSpace);

  public ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TAlgorithmState, TSearchSpace, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return terminator.ShouldTerminate(state, terminatorState, searchSpace);
    }
  }
}

public abstract record StatefulTerminator<TGenotype, TAlgorithmState, TState>
  : ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : class, IAlgorithmState
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState state, TState terminatorState);

  public ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TAlgorithmState, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public bool ShouldTerminate(TAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return terminator.ShouldTerminate(state, terminatorState);
    }
  }
}

public abstract record StatefulTerminator<TGenotype, TState>
  : ITerminator<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TState terminatorState);

  public ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulTerminatorInstance(this, CreateInitialState());

  private sealed class StatefulTerminatorInstance(StatefulTerminator<TGenotype, TState> terminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public bool ShouldTerminate(IAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return terminator.ShouldTerminate(terminatorState);
    }
  }
}

