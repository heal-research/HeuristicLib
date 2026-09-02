using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <summary>
/// A move applier configuration. It names the candidate representation it transforms and the kind of move it applies;
/// the search space and problem types are supplied when an execution instance is created.
/// </summary>
public interface IMoveApplier<TCandidate, in TMove> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because a move applier's own search space and problem, where it has them,
    /// are its type arguments and mean something different: what it was written for, rather than what it is being
    /// asked to run over.
    /// </remarks>
    IMoveApplierInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveApplierInstance<TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TCandidate Apply(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

/// <summary>
/// Resolution for the move applier role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class MoveApplierResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a move applier over a triple named at this call site.</summary>
        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveApplier<TCandidate, TMove> moveApplier)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(moveApplier, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a move applier that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveApplier<TCandidate, TMove>? moveApplier)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveApplier is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);

        /// <summary>
        /// Resolves a move applier, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveApplier<TCandidate, TMove> moveApplier,
            [NotNullWhen(true)] out IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);
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
        /// <summary>Resolves a move applier over the triple this resolver already carries.</summary>
        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveApplier<TCandidate, TMove> moveApplier) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);

        /// <summary>Resolves a move applier that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveApplier<TCandidate, TMove>? moveApplier) =>
            moveApplier is null ? null : resolver.Resolve(moveApplier);

        /// <summary>
        /// Resolves a move applier, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve<TMove>(
            IMoveApplier<TCandidate, TMove> moveApplier,
            [NotNullWhen(true)] out IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier, out instance, out reason);
    }
}
