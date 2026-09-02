using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// An evaluator configuration. It names only the candidate representation; the search space and problem types are
/// supplied when an execution instance is created.
/// </summary>
public interface IEvaluator<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IEvaluatorInstance<TCandidate, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the evaluator role, declared next to the role because the registry cannot name a role generically.
/// </summary>
public static class EvaluatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves an evaluator over a triple named at this call site.</summary>
        public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> evaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(evaluator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>Resolves an evaluator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate>? evaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            evaluator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);

        /// <summary>
        /// Resolves an evaluator, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>See <see cref="CrossoverResolverExtensions"/> for why this is the binding check.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate> evaluator,
            [NotNullWhen(true)] out IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);
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
        /// <summary>Resolves an evaluator over the triple this resolver already carries.</summary>
        public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> Resolve(IEvaluator<TCandidate> evaluator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);

        /// <summary>Resolves an evaluator that may be absent, returning <see langword="null"/> when it is.</summary>
        public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IEvaluator<TCandidate>? evaluator) =>
            evaluator is null ? null : resolver.Resolve(evaluator);

        /// <summary>
        /// Resolves an evaluator, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            IEvaluator<TCandidate> evaluator,
            [NotNullWhen(true)] out IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(evaluator, out instance, out reason);
    }
}
