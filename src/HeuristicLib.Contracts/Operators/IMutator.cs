using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// A mutator configuration. It names only the candidate representation; the search space and problem types are
/// supplied when an execution instance is created.
/// </summary>
public interface IMutator<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMutatorInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the mutator role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class MutatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a mutator over a triple named at this call site.</summary>
        public IMutatorInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate> mutator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(mutator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a mutator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMutatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate>? mutator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            mutator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(mutator);

        /// <summary>
        /// Resolves a mutator, reporting rather than throwing when it cannot run over this search space and problem.
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
            IMutator<TCandidate> mutator,
            [NotNullWhen(true)] out IMutatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(mutator);
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
        /// <summary>Resolves a mutator over the triple this resolver already carries.</summary>
        public IMutatorInstance<TCandidate, TSearchSpace, TProblem> Resolve(IMutator<TCandidate> mutator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(mutator);

        /// <summary>Resolves a mutator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IMutatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IMutator<TCandidate>? mutator) =>
            mutator is null ? null : resolver.Resolve(mutator);

        /// <summary>
        /// Resolves a mutator, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            IMutator<TCandidate> mutator,
            [NotNullWhen(true)] out IMutatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(mutator, out instance, out reason);
    }
}
