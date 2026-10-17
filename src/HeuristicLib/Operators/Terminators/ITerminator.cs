using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <remarks>
/// A terminator written for a search space, problem or state the run does not supply is reported when the execution
/// graph is built.
/// </remarks>
public interface ITerminator<TCandidate> : IOperator
{
    ExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface ITerminationExecutor<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public abstract class TerminatorAdvice<TCandidate> : Advice<ITerminator<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="terminator">The configuration the pointcut selected.</param>
    public abstract WrapperExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminator<TCandidate> terminator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public static class TerminatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate> terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            terminator is IOperator<Termination, TCandidate> prepared
                ? scope.ResolveOperator<Termination, TCandidate, TSearchSpace, TProblem, TSearchState, ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>>(
                    prepared, Termination.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()).Executor
                : scope.Resolve(terminator, static source => source.CreateExecutorFactory<TSearchSpace, TProblem, TSearchState>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TSearchState>);

        [return: NotNullIfNotNull(nameof(terminator))]
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate>? terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            terminator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                executor = null;
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
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(ITerminator<TCandidate> terminator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        [return: NotNullIfNotNull(nameof(terminator))]
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(ITerminator<TCandidate>? terminator) =>
            terminator is null ? null : scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public bool TryResolve(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(terminator, out executor, out reason);
    }

    private static WrapperExecutorFactory<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TSearchState>(Advice advice, ITerminator<TCandidate> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        ((TerminatorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem, TSearchState>(terminator);
}
