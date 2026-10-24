using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMoveEvaluationExecutor<in TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    ObjectiveVector Evaluate(
        ObjectiveVector oldQuality,
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    ObjectiveVector Evaluate(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

public static class MoveEvaluationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveEvaluation<TMove>, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveEvaluation<TMove>, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveEvaluation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IOperator<MoveEvaluation<TMove>, TCandidate> source) =>
            (IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>)scope.Scope.ResolveOperator<MoveEvaluation<TMove>, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IOperator<MoveEvaluation<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveEvaluation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IOperator<MoveEvaluation<TMove>, TCandidate> source) =>
            (IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>)scope.Scope.ResolveOperator<MoveEvaluation<TMove>, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IOperator<MoveEvaluation<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveEvaluation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
