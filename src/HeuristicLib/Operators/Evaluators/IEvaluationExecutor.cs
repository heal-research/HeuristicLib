using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluationExecutor<TCandidate> : IOperatorExecutor
{
    IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}

public static class EvaluationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IEvaluationExecutor<TCandidate> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IEvaluationExecutor<TCandidate>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out IEvaluationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IEvaluationExecutor<TCandidate> Resolve(IOperator<Evaluation, TCandidate> source) =>
            (IEvaluationExecutor<TCandidate>)scope.ResolveOperator<Evaluation, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);

        public IEvaluationExecutor<TCandidate> Resolve<TControl>(IOperator<Evaluation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IEvaluationExecutor<TCandidate>)scope.ResolveOperator<Evaluation, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IEvaluationExecutor<TCandidate>? ResolveOptional(IOperator<Evaluation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Evaluation, TCandidate> source,
            [NotNullWhen(true)] out IEvaluationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IEvaluationExecutor<TCandidate> Resolve(IOperator<Evaluation, TCandidate> source) =>
            (IEvaluationExecutor<TCandidate>)scope.ResolveOperator<Evaluation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);

        public IEvaluationExecutor<TCandidate> Resolve<TControl>(IOperator<Evaluation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IEvaluationExecutor<TCandidate>)scope.ResolveOperator<Evaluation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IEvaluationExecutor<TCandidate>? ResolveOptional(IOperator<Evaluation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Evaluation, TCandidate> source,
            [NotNullWhen(true)] out IEvaluationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
