using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public interface IMoveEvaluator<TCandidate, in TMove> : IOperator
{
    IMoveEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveEvaluatorExecution<in TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorExecution
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
        public IMoveEvaluatorExecution<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(moveEvaluator, static (creationTarget, childScope) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childScope));

        public IMoveEvaluatorExecution<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveEvaluator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluatorExecution<TCandidate, TSearchSpace, TProblem, TMove>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);
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

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveEvaluatorExecution<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        public IMoveEvaluatorExecution<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator) =>
            moveEvaluator is null ? null : scope.Resolve(moveEvaluator);

        public bool TryResolve<TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluatorExecution<TCandidate, TSearchSpace, TProblem, TMove>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(moveEvaluator, out execution, out reason);
    }
}
