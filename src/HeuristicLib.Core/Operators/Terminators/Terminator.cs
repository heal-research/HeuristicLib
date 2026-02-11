using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

// Move Terminators out of Operators?

public abstract record Terminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  {
    public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
  }
}


public abstract record Terminator<TGenotype, TAlgorithmState, TSearchSpace>
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace);

    bool ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      ShouldTerminate(state, searchSpace);
  }
}



public abstract record Terminator<TGenotype, TAlgorithmState>
  : ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public abstract bool ShouldTerminate(TAlgorithmState state);

    bool ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(TAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      ShouldTerminate(state);
  }
}

public abstract record Terminator<TGenotype>
  : ITerminator<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public abstract bool ShouldTerminate();

    bool ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(IAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      ShouldTerminate();
  }
}
