using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ISelectionExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class SelectionOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Selection, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Selection, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Selection, TCandidate> source,
            [NotNullWhen(true)] out ISelectionExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Selection, TCandidate> source) =>
            (ISelectionExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Selection, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Selection, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ISelectionExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Selection, TCandidate, TSearchSpace, TProblem, ISearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Selection, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Selection, TCandidate> source,
            [NotNullWhen(true)] out ISelectionExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Selection, TCandidate> source) =>
            (ISelectionExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Selection, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Selection, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ISelectionExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Selection, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ISelectionExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Selection, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Selection, TCandidate> source,
            [NotNullWhen(true)] out ISelectionExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
