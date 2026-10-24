using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public readonly record struct Parents<T>(T Parent1, T Parent2);

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

public static class CrossoverOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Crossover, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Crossover, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Crossover, TCandidate> source,
            [NotNullWhen(true)] out ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Crossover, TCandidate> source) =>
            (ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Crossover, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Crossover, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Crossover, TCandidate, TSearchSpace, TProblem, ISearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Crossover, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Crossover, TCandidate> source,
            [NotNullWhen(true)] out ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Crossover, TCandidate> source) =>
            (ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Crossover, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Crossover, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Crossover, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Crossover, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Crossover, TCandidate> source,
            [NotNullWhen(true)] out ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
