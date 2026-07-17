using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> InnerTerminator { get; }

    protected WrappingTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator)
    {
        InnerTerminator = innerTerminator;
    }

    protected sealed override ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(IExecutionInstanceResolver resolver) =>
        CreateTerminatorInstance(resolver.Resolve(InnerTerminator));

    protected abstract WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator);
}

public abstract class WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerTerminator)
    : TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> InnerTerminator { get; } = innerTerminator;
}
