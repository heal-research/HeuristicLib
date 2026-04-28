using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatelessTerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>,
    ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessTerminator<TGenotype, TSearchState, TSearchSpace>
  : ITerminator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>,
    ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate(TSearchState state, TSearchSpace searchSpace);

  bool ITerminatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>.ShouldTerminate(TSearchState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
    ShouldTerminate(state, searchSpace);
}

public abstract record StatelessTerminator<TGenotype, TSearchState>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>,
    ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>
  where TSearchState : class, ISearchState
{
  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate(TSearchState state);

  bool ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>.ShouldTerminate(TSearchState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    ShouldTerminate(state);
}

public abstract record StatelessTerminator<TGenotype>
  : ITerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState>,
    ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState>
{
  public ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract bool ShouldTerminate();

  bool ITerminatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, ISearchState>.ShouldTerminate(ISearchState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    ShouldTerminate();
}
