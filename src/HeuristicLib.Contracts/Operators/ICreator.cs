using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// A creator configuration. It names only the candidate representation; the search space and problem types are
/// supplied when an execution instance is created.
/// </summary>
public interface ICreator<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ICreatorInstance<TCandidate, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the creator role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class CreatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a creator over a triple named at this call site.</summary>
        public ICreatorInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(creator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a creator that may be absent, returning <see langword="null"/> when it is.</summary>
        public ICreatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate>? creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            creator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        /// <summary>
        /// Resolves a creator, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>
        /// This is the binding check. An operator states the search space and problem it was written for on its own
        /// type, so whether it can serve a given run is answered by asking it for an execution instance — there is no
        /// cheaper question to ask, and a second rule engine beside it could only drift.
        /// <para>
        /// A <see langword="true"/> result carries the very instance the run will use, and the registry has stored it,
        /// so validating and creating are one step: a configuration that validates cannot fail later for this reason.
        /// </para>
        /// </remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(creator);
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
        /// <summary>Resolves a creator over the triple this resolver already carries.</summary>
        public ICreatorInstance<TCandidate, TSearchSpace, TProblem> Resolve(ICreator<TCandidate> creator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        /// <summary>Resolves a creator that may be absent, returning <see langword="null"/> when it is.</summary>
        public ICreatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ICreator<TCandidate>? creator) =>
            creator is null ? null : resolver.Resolve(creator);

        /// <summary>
        /// Resolves a creator, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(creator, out instance, out reason);
    }
}
