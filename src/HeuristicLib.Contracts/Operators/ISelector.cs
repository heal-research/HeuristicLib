using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// A selector configuration. It names only the candidate representation; the search space and problem types are
/// supplied when an execution instance is created.
/// </summary>
public interface ISelector<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ISelectorInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the selector role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class SelectorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a selector over a triple named at this call site.</summary>
        public ISelectorInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> selector)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(selector, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a selector that may be absent, returning <see langword="null"/> when it is.</summary>
        public ISelectorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate>? selector)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            selector is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(selector);

        /// <summary>
        /// Resolves a selector, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>See <see cref="CrossoverResolverExtensions"/> for why this is the binding check.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ISelector<TCandidate> selector,
            [NotNullWhen(true)] out ISelectorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(selector);
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
        /// <summary>Resolves a selector over the triple this resolver already carries.</summary>
        public ISelectorInstance<TCandidate, TSearchSpace, TProblem> Resolve(ISelector<TCandidate> selector) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(selector);

        /// <summary>Resolves a selector that may be absent, returning <see langword="null"/> when it is.</summary>
        public ISelectorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ISelector<TCandidate>? selector) =>
            selector is null ? null : resolver.Resolve(selector);

        /// <summary>
        /// Resolves a selector, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            ISelector<TCandidate> selector,
            [NotNullWhen(true)] out ISelectorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(selector, out instance, out reason);
    }
}
