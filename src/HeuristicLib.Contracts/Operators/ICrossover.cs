using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public readonly record struct Parents<T>(T Parent1, T Parent2);

/// <summary>
/// A crossover configuration. It names only the candidate representation; the search space and problem types are
/// supplied when an execution instance is created.
/// </summary>
public interface ICrossover<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space and problem a run supplies.
    /// </summary>
    /// <remarks>
    /// The type arguments are named for the run because an operator's own search space and problem, where it has
    /// them, are its type arguments and mean something different: what it was written for, rather than what it is
    /// being asked to run over.
    /// </remarks>
    ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ICrossoverInstance<TCandidate, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class Parents
{
    public static Parents<T> From<T>(T parent1, T parent2) => new(parent1, parent2);
}

public static class ParentsExtensions
{
    extension<T>((T Parent1, T Parent2) parents)
    {
        public Parents<T> ToParents() => Parents.From(parents.Parent1, parents.Parent2);
    }

    extension<TCandidate>(IReadOnlyList<TCandidate> parents)
    {
        public IReadOnlyList<Parents<TCandidate>> ToParentPairs()
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new Parents<TCandidate>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                var p1 = parents[j];
                var p2 = parents[j + 1];
                parentPairs[i] = Parents.From(p1, p2);
            }

            return parentPairs;
        }
    }

    extension<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> parents)
    {
        public Parents<TCandidate>[] ToParents(ObjectiveDirections? objective = null)
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new Parents<TCandidate>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                var p1 = parents[j];
                var p2 = parents[j + 1];
                if (objective is not null
                    && objective.TotalOrderComparer is not NoTotalOrderComparer
                    && objective.TotalOrderComparer.Compare(p1.ObjectiveVector, p2.ObjectiveVector) > 0)
                    (p1, p2) = (p2, p1);
                parentPairs[i] = Parents.From(p1.Candidate, p2.Candidate);
            }

            return parentPairs;
        }

        public Parents<EvaluatedCandidate<TCandidate>>[] ToEvaluatedCandidatesPairs()
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new Parents<EvaluatedCandidate<TCandidate>>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                parentPairs[i] = Parents.From(parents[j], parents[j + 1]);
            }

            return parentPairs;
        }
    }
}

/// <summary>
/// Resolution for the crossover role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. A resolver built with
/// <see cref="ExecutionInstanceRegistryResolverExtensions.For{TCandidate, TSearchSpace, TProblem}"/> is optional
/// sugar over exactly this, for a call site that resolves several operators and would otherwise repeat the triple.
/// </remarks>
public static class CrossoverResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a crossover over a triple named at this call site.</summary>
        public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> crossover)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(crossover, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        /// <summary>
        /// Resolves a crossover, reporting rather than throwing when it cannot run over this search space and problem.
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
            ICrossover<TCandidate> crossover,
            [NotNullWhen(true)] out ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem>(crossover);
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

        /// <summary>Resolves a crossover that may be absent, returning <see langword="null"/> when it is.</summary>
        /// <remarks>For an optional slot such as the crossover of an evolution strategy.</remarks>
        public ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate>? crossover)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            crossover is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem>(crossover);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        /// <summary>Resolves a crossover over the triple this resolver already carries.</summary>
        public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> Resolve(ICrossover<TCandidate> crossover) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem>(crossover);

        /// <summary>Resolves a crossover that may be absent, returning <see langword="null"/> when it is.</summary>
        public ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ICrossover<TCandidate>? crossover) =>
            crossover is null ? null : resolver.Resolve(crossover);

        /// <summary>
        /// Resolves a crossover, reporting rather than throwing when it cannot run over this resolver's search space
        /// and problem.
        /// </summary>
        public bool TryResolve(
            ICrossover<TCandidate> crossover,
            [NotNullWhen(true)] out ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem>(crossover, out instance, out reason);
    }
}
