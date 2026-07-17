using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public abstract partial record MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> InnerTerminators { get; }

    protected MultiTerminator(ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators)
    {
        InnerTerminators = innerTerminators;
    }

    protected sealed override ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(IExecutionInstanceResolver resolver) =>
        CreateTerminatorInstance([.. InnerTerminators.Select(resolver.Resolve)]);

    protected abstract MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators);
}

public abstract class MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators)
    : TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> InnerTerminators { get; } = innerTerminators;
}
