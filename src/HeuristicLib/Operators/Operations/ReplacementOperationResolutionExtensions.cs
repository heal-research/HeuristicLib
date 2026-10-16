using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public static class ReplacementOperationResolutionExtensions
{
    public static IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Replacement> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.Scope.ResolveOperator<TCandidate, Replacement, TSearchSpace, TProblem, ISearchState,
            IReplacementExecutor<TCandidate, TSearchSpace, TProblem>>(source, Replacement.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>()).Executor;

    public static IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Replacement> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Replacement, TSearchSpace, TProblem, ISearchState,
            IReplacementExecutor<TCandidate, TSearchSpace, TProblem>>(source, Replacement.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>());
        control = bound.ControlProvider.Get<TControl>();
        return bound.Executor;
    }

    public static IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Replacement> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        scope.Scope.ResolveOperator<TCandidate, Replacement, TSearchSpace, TProblem, TRunSearchState,
            IReplacementExecutor<TCandidate, TSearchSpace, TProblem>>(source, Replacement.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()).Executor;

    public static IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Replacement> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Replacement, TSearchSpace, TProblem, TRunSearchState,
            IReplacementExecutor<TCandidate, TSearchSpace, TProblem>>(source, Replacement.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>());
        control = bound.ControlProvider.Get<TControl>();
        return bound.Executor;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Replacement>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => source is null ? null : scope.Resolve(source);

    [return: NotNullIfNotNull(nameof(source))]
    public static IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Replacement>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Replacement> source,
        [NotNullWhen(true)] out IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        try
        {
            executor = scope.Resolve(source);
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

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Replacement> source,
        [NotNullWhen(true)] out IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        try
        {
            executor = scope.Resolve(source);
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
