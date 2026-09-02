using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// Derive directly from this base when the terminator owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessTerminator{TCandidate,TSearchSpace,TProblem,TSearchState}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulTerminator{TCandidate,TSearchSpace,TProblem,TSearchState,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : ITerminator<TCandidate>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

    ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> ITerminator<TCandidate>.CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name}, {typeof(TProblem).Name} and {typeof(TSearchState).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name} and {typeof(TRunSearchState).Name}.");
        }

        return instance;
    }
}

public abstract record Terminator<TCandidate, TSearchSpace, TSearchState>
    : Terminator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Terminator<TCandidate, TSearchState>
    : Terminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState;

public abstract record Terminator<TCandidate>
    : Terminator<TCandidate, ISearchState>;

public abstract class TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public abstract class TerminatorInstance<TCandidate, TSearchSpace, TSearchState>
    : ITerminatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract bool IsTerminalState(TSearchState state, TSearchSpace searchSpace);

    bool ITerminatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.IsTerminalState(TSearchState state, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        IsTerminalState(state, searchSpace);
}

public abstract class TerminatorInstance<TCandidate, TSearchState>
    : ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract bool IsTerminalState(TSearchState state);

    bool ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.IsTerminalState(TSearchState state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        IsTerminalState(state);
}

public abstract class TerminatorInstance<TCandidate>
    : ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>
{
    public abstract bool IsTerminalState();

    bool ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>.IsTerminalState(ISearchState state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        IsTerminalState();
}
