using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public interface IMoveEvaluator<TCandidate, in TMove> : IOperator
{
    ExecutorFactory<IMoveEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TMove>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

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

public static class MoveEvaluatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(moveEvaluator, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TMove>);

        [return: NotNullIfNotNull(nameof(moveEvaluator))]
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveEvaluator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);
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

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        [return: NotNullIfNotNull(nameof(moveEvaluator))]
        public IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator) =>
            moveEvaluator is null ? null : scope.Resolve(moveEvaluator);

        public bool TryResolve<TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(moveEvaluator, out executor, out reason);
    }

    private static WrapperExecutorFactory<IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TMove>(Advice advice, IMoveEvaluator<TCandidate, TMove> configuration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => throw new InvalidOperationException($"{advice.GetType().Name} is not a single-child wrapper for {typeof(IMoveEvaluator<TCandidate, TMove>).Name} producing {typeof(IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>).Name}.");
}
