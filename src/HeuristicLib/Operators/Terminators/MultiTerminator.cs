using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public abstract partial record MultiTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, TExecutionState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>> InnerTerminators { get; }

    protected delegate bool InnerIsTerminalState(TSearchState searchState, TSearchSpace searchSpace, TProblem problem);

    protected MultiTerminator(ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>> innerTerminators)
    {
        InnerTerminators = innerTerminators;
    }

    public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerTerminators.Select(instanceRegistry.Resolve).Select(x => (InnerIsTerminalState)x.IsTerminalState).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TExecutionState executionState,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, TExecutionState> multiTerminator,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TExecutionState executionState)
      : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
    {
        public bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            return multiTerminator.IsTerminalState(state, executionState, innerTerminators, searchSpace, problem);
        }
    }
}

public abstract record MultiTerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  : MultiTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    protected MultiTerminator(ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>> innerTerminators)
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
