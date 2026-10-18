using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public readonly record struct Parents<T>(T Parent1, T Parent2);

public interface ICrossover<TCandidate> : IOperator
{
    ExecutorFactory<ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ICrossoverExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
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

public abstract class CrossoverAdvice<TCandidate> : Advice<ICrossover<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="crossover">The configuration the pointcut selected.</param>
    public abstract WrapperExecutorFactory<ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICrossover<TCandidate> crossover)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class CrossoverResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> crossover)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            crossover is IOperator<Crossover, TCandidate> prepared
                ? (ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>)scope.ResolveOperator<Crossover, TCandidate, TSearchSpace, TProblem, ISearchState>(prepared)
                : scope.Resolve(crossover, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        /// <remarks>A true result carries the instance the run will use, so validating and creating are one step.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate> crossover,
            [NotNullWhen(true)] out ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem>(crossover);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                executor = null;
                reason = exception.Message;
                return false;
            }
        }

        /// <remarks>For an optional slot such as the crossover of an evolution strategy.</remarks>
        [return: NotNullIfNotNull(nameof(crossover))]
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate>? crossover)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            crossover is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(crossover);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve(ICrossover<TCandidate> crossover) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(crossover);

        [return: NotNullIfNotNull(nameof(crossover))]
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ICrossover<TCandidate>? crossover) =>
            crossover is null ? null : scope.Resolve(crossover);

        public bool TryResolve(
            ICrossover<TCandidate> crossover,
            [NotNullWhen(true)] out ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(crossover, out executor, out reason);
    }

    private static WrapperExecutorFactory<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, ICrossover<TCandidate> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((CrossoverAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(crossover);
}
