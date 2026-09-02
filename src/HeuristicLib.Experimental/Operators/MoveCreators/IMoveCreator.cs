using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

/// <summary>
/// A move creator configuration. It names the candidate representation it walks and the kind of move it yields; the
/// search space and problem types are supplied when an execution instance is created.
/// </summary>
public interface IMoveCreator<TCandidate, out TMove> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because a move creator's own search space and problem, where it has them,
    /// are its type arguments and mean something different: what it was written for, rather than what it is being
    /// asked to run over.
    /// </remarks>
    IMoveCreatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveCreatorInstance<in TCandidate, in TSearchSpace, in TProblem, out TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IEnumerable<TMove> Moves(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

/// <summary>
/// Resolution for the move creator role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class MoveCreatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a move creator over a triple named at this call site.</summary>
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveCreator<TCandidate, TMove> moveCreator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(moveCreator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a move creator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveCreator<TCandidate, TMove>? moveCreator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveCreator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);

        /// <summary>
        /// Resolves a move creator, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveCreator<TCandidate, TMove> moveCreator,
            [NotNullWhen(true)] out IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);
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
        /// <summary>Resolves a move creator over the triple this resolver already carries.</summary>
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveCreator<TCandidate, TMove> moveCreator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);

        /// <summary>Resolves a move creator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveCreator<TCandidate, TMove>? moveCreator) =>
            moveCreator is null ? null : resolver.Resolve(moveCreator);

        /// <summary>
        /// Resolves a move creator, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve<TMove>(
            IMoveCreator<TCandidate, TMove> moveCreator,
            [NotNullWhen(true)] out IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator, out instance, out reason);
    }
}
