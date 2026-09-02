using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// The operator role that works on existing candidates to improve a chosen aspect of them. Common refinements fit
/// candidate parameters, repair invalid candidates, simplify or normalize a representation, and apply local-improvement
/// procedures.
/// </summary>
/// <remarks>
/// Algorithms normally refine newly created or varied candidates before evaluating them, and then continue the search
/// with the refined candidates. Refinement is composed through the usual operator topologies: <c>PipelineRefiner</c>
/// runs an ordered sequence such as repair, simplification and constant optimization, <c>IteratedRefiner</c> repeats
/// one refinement, and <c>ChooseOneRefiner</c> applies one alternative per candidate.
/// <para>
/// A refiner differs from a mutator by intent. A mutator perturbs a candidate to create variation, while a refiner
/// moves a candidate towards a better one along a specific dimension of quality. Where objective values guide the
/// refinement or decide whether to keep its result, the refiner takes an
/// <see cref="IEvaluator{TCandidate,TSearchSpace,TProblem}"/> as a configured dependency, which keeps those evaluations
/// visible to budgets, termination, caching and analysis.
/// </para>
/// <para>
/// Continuing the search with the refined candidate is Lamarckian refinement. <c>RefinementEvaluator</c> instead
/// measures a transient refined copy and reports its objective vector for the original candidate, which is Baldwinian.
/// </para>
/// </remarks>
public interface IRefiner<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IRefinerInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the refiner role, declared next to the role because the registry cannot name a role generically.
/// </summary>
public static class RefinerResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a refiner over a triple named at this call site.</summary>
        public IRefinerInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> refiner)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(refiner, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves a refiner that may be absent, returning <see langword="null"/> when it is.</summary>
        public IRefinerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate>? refiner)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            refiner is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);

        /// <summary>
        /// Resolves a refiner, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>See <see cref="CrossoverResolverExtensions"/> for why this is the binding check.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IRefiner<TCandidate> refiner,
            [NotNullWhen(true)] out IRefinerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);
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
        /// <summary>Resolves a refiner over the triple this resolver already carries.</summary>
        public IRefinerInstance<TCandidate, TSearchSpace, TProblem> Resolve(IRefiner<TCandidate> refiner) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);

        /// <summary>Resolves a refiner that may be absent, returning <see langword="null"/> when it is.</summary>
        public IRefinerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IRefiner<TCandidate>? refiner) =>
            refiner is null ? null : resolver.Resolve(refiner);

        /// <summary>
        /// Resolves a refiner, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            IRefiner<TCandidate> refiner,
            [NotNullWhen(true)] out IRefinerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(refiner, out instance, out reason);
    }
}
