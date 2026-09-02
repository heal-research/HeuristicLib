using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// A replacer configuration. It names only the candidate representation; the search space and problem types are
/// supplied when an execution instance is created.
/// </summary>
public interface IReplacer<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IReplacerInstance<TCandidate, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
      ObjectiveDirections objective, int count,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the replacer role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class ReplacerResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a replacer over a triple named at this call site.</summary>
        public IReplacerInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(replacer, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a replacer that may be absent, returning <see langword="null"/> when it is.</summary>
        public IReplacerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate>? replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            replacer is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        /// <summary>
        /// Resolves a replacer, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>See <see cref="CrossoverResolverExtensions"/> for why this is the binding check.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);
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
        /// <summary>Resolves a replacer over the triple this resolver already carries.</summary>
        public IReplacerInstance<TCandidate, TSearchSpace, TProblem> Resolve(IReplacer<TCandidate> replacer) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        /// <summary>Resolves a replacer that may be absent, returning <see langword="null"/> when it is.</summary>
        public IReplacerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IReplacer<TCandidate>? replacer) =>
            replacer is null ? null : resolver.Resolve(replacer);

        /// <summary>
        /// Resolves a replacer, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(replacer, out instance, out reason);
    }
}
