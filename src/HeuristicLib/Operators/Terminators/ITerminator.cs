using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <remarks>
/// A terminator written for a search space, problem or state the run does not supply is reported when the execution
/// graph is built.
/// </remarks>
public interface ITerminator<TCandidate> : IOperator
{
    ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface ITerminatorInstance<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public static class TerminatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate> terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            scope.Resolve(terminator, static (creationTarget, childScope) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childScope));

        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate>? terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            terminator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                instance = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                instance = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(ITerminator<TCandidate> terminator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(ITerminator<TCandidate>? terminator) =>
            terminator is null ? null : scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public bool TryResolve(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(terminator, out instance, out reason);
    }
}
