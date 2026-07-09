using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public abstract partial record MultiTerminator<TCandidate, TSearchState, TSearchSpace, TProblem, TExecutionState>
  : ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> InnerTerminators { get; }

    protected delegate bool InnerIsTerminalState(TSearchState searchState, TSearchSpace searchSpace, TProblem problem);

    protected MultiTerminator(ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators)
    {
        InnerTerminators = innerTerminators;
    }

    public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerTerminators.Select(instanceRegistry.Resolve).Select(x => (InnerIsTerminalState)x.IsTerminalState).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TExecutionState executionState,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiTerminator<TCandidate, TSearchState, TSearchSpace, TProblem, TExecutionState> multiTerminator,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TExecutionState executionState)
      : ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            return multiTerminator.IsTerminalState(state, executionState, innerTerminators, searchSpace, problem);
        }
    }
}

public abstract record MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  : MultiTerminator<TCandidate, TSearchState, TSearchSpace, TProblem, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiTerminator(ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators)
      : base(innerTerminators)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override bool IsTerminalState(TSearchState searchState, NoState executionState,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TSearchSpace searchSpace, TProblem problem)
      => IsTerminalState(searchState, innerTerminators, searchSpace, problem);

    protected abstract bool IsTerminalState(TSearchState searchState,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TSearchSpace searchSpace, TProblem problem);
}
