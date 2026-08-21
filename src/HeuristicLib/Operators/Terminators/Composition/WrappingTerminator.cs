using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator)
    {
        ChildTerminator = childTerminator;
    }

    public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> ChildTerminator { get; init; }

    public sealed override ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildTerminator));

    protected abstract WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator);
}

public abstract class WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator)
    : TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> ChildTerminator { get; } = childTerminator;
}
