using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record WrappingTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, TExecutionState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    protected delegate bool InnerIsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);

    protected ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState> InnerTerminator { get; }

    protected WrappingTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState> innerTerminator)
    {
        InnerTerminator = innerTerminator;
    }

    public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, instanceRegistry.Resolve(InnerTerminator).IsTerminalState, CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TExecutionState executionState,
      InnerIsTerminalState innerIsTerminalState,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(WrappingTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, TExecutionState> wrappingTerminator,
      InnerIsTerminalState innerIsTerminalState, TExecutionState executionState)
      : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
    {
        public bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingTerminator.IsTerminalState(state, executionState, innerIsTerminalState, searchSpace, problem);
        }
    }
}

public abstract record WrappingTerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  : WrappingTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    protected WrappingTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState> innerTerminator)
      : base(innerTerminator)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override bool IsTerminalState(TSearchState searchState, NoState executionState,
      InnerIsTerminalState innerIsTerminalState,
      TSearchSpace searchSpace, TProblem problem)
      => IsTerminalState(searchState, innerIsTerminalState, searchSpace, problem);

    protected abstract bool IsTerminalState(TSearchState searchState,
      InnerIsTerminalState innerIsTerminalState,
      TSearchSpace searchSpace, TProblem problem);
}
