using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

/// <summary>
/// A move evaluator configuration. It names the candidate representation it scores and the kind of move it scores a
/// step of; the search space and problem types are supplied when an execution instance is created.
/// </summary>
public interface IMoveEvaluator<TCandidate, in TMove> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because a move evaluator's own search space and problem, where it has them,
    /// are its type arguments and mean something different: what it was written for, rather than what it is being
    /// asked to run over.
    /// </remarks>
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

/// <summary>
/// Resolution for the move evaluator role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class MoveEvaluatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a move evaluator over a triple named at this call site.</summary>
        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(moveEvaluator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a move evaluator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveEvaluator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        /// <summary>
        /// Resolves a move evaluator, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
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
        /// <summary>Resolves a move evaluator over the triple this resolver already carries.</summary>
        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveEvaluator<TCandidate, TMove> moveEvaluator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator);

        /// <summary>Resolves a move evaluator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveEvaluator<TCandidate, TMove>? moveEvaluator) =>
            moveEvaluator is null ? null : resolver.Resolve(moveEvaluator);

        /// <summary>
        /// Resolves a move evaluator, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve<TMove>(
            IMoveEvaluator<TCandidate, TMove> moveEvaluator,
            [NotNullWhen(true)] out IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(moveEvaluator, out instance, out reason);
    }
}
