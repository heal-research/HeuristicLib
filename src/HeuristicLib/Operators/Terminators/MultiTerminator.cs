using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiTerminator(IReadOnlyList<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
    {
        ChildTerminators = childTerminators.ToValueArray();
    }

    public ValueArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildTerminators { get; init; }

    public sealed override ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildTerminators.Select(instanceRegistry.Resolve)]);

    protected abstract MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators);
}

public abstract class MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
    : TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildTerminators { get; } = childTerminators;
}
