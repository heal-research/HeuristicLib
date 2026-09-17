using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public interface IMoveEvaluator<TCandidate, in TMove> : IOperator
{
    IMoveEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveEvaluatorInstance<in TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorInstance
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

public static class MoveEvaluatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(moveEvaluator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveEvaluator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);
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

    extension<TCandidate, TSearchSpace, TProblem>(ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator) =>
            moveEvaluator is null ? null : resolver.Resolve(moveEvaluator);

        public bool TryResolve<TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve(moveEvaluator, out instance, out reason);
    }
}
