using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorRolePrototype;

public static class OperatorResolutionExtensions
{
    public static IMutatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Mutation> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.Scope.ResolveOperator<TCandidate, Mutation, TSearchSpace, TProblem, ISearchState,
            IMutatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Mutation.CreateBinding<TCandidate, TSearchSpace, TProblem, ISearchState>()).Operation;

    public static IMutatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Mutation> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Mutation, TSearchSpace, TProblem, ISearchState,
            IMutatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Mutation.CreateBinding<TCandidate, TSearchSpace, TProblem, ISearchState>());
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static IMutatorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Mutation>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Mutation> source,
        [NotNullWhen(true)] out IMutatorExecution<TCandidate, TSearchSpace, TProblem>? execution, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        try
        {
            execution = scope.Resolve(source);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    public static IMutatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Mutation> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        scope.Scope.ResolveOperator<TCandidate, Mutation, TSearchSpace, TProblem, TRunSearchState,
            IMutatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Mutation.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()).Operation;

    public static IMutatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Mutation> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Mutation, TSearchSpace, TProblem, TRunSearchState,
            IMutatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Mutation.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>());
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static IMutatorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Mutation>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Mutation> source,
        [NotNullWhen(true)] out IMutatorExecution<TCandidate, TSearchSpace, TProblem>? execution, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        try
        {
            execution = scope.Resolve(source);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    public static IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Evaluation> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.Scope.ResolveOperator<TCandidate, Evaluation, TSearchSpace, TProblem, ISearchState,
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Evaluation.CreateBinding<TCandidate, TSearchSpace, TProblem, ISearchState>()).Operation;

    public static IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Evaluation> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Evaluation, TSearchSpace, TProblem, ISearchState,
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Evaluation.CreateBinding<TCandidate, TSearchSpace, TProblem, ISearchState>());
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Evaluation>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Evaluation> source,
        [NotNullWhen(true)] out IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>? execution, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        try
        {
            execution = scope.Resolve(source);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    public static IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Evaluation> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        scope.Scope.ResolveOperator<TCandidate, Evaluation, TSearchSpace, TProblem, TRunSearchState,
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Evaluation.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()).Operation;

    public static IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Evaluation> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Evaluation, TSearchSpace, TProblem, TRunSearchState,
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>(source, Evaluation.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>());
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Evaluation>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Evaluation> source,
        [NotNullWhen(true)] out IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>? execution, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        try
        {
            execution = scope.Resolve(source);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    public static ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Termination> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        scope.Scope.ResolveOperator<TCandidate, Termination, TSearchSpace, TProblem, TRunSearchState,
            ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>(source, Termination.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()).Operation;

    public static ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Termination> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Termination, TSearchSpace, TProblem, TRunSearchState,
            ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>(source, Termination.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>());
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Termination>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Termination> source,
        [NotNullWhen(true)] out ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>? execution, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        try
        {
            execution = scope.Resolve(source);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    public static IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Interception> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        scope.Scope.ResolveOperator<TCandidate, Interception, TSearchSpace, TProblem, TRunSearchState,
            IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>(source, Interception.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()).Operation;

    public static IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState, TControl>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Interception> source, out TControl? control)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
        where TControl : class
    {
        var bound = scope.Scope.ResolveOperator<TCandidate, Interception, TSearchSpace, TProblem, TRunSearchState,
            IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>(source, Interception.CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>());
        control = bound.Controls.Get<TControl>();
        return bound.Operation;
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Interception>? source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState => source is null ? null : scope.Resolve(source);

    public static bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope, IOperator<TCandidate, Interception> source,
        [NotNullWhen(true)] out IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>? execution, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        try
        {
            execution = scope.Resolve(source);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

}

