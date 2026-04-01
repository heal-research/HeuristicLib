using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatelessTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>,
    ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessTerminator<TGenotype, TAlgorithmState, TSearchSpace>
  : ITerminator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>,
    ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace);

  bool ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>.ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
    ShouldTerminate(state, searchSpace);
}

public abstract record StatelessTerminator<TGenotype, TAlgorithmState>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>,
    ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
{
  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate(TAlgorithmState state);

  bool ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>.ShouldTerminate(TAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    ShouldTerminate(state);
}

public abstract record StatelessTerminator<TGenotype>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>,
    ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>
{
  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate();

  bool ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>.ShouldTerminate(IAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    ShouldTerminate();
}
