using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ICreationExecutor<TCandidate> : IOperatorExecutor
{
    IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);
}

public static class CreationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ICreationExecutor<TCandidate> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Creation, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ICreationExecutor<TCandidate>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Creation, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Creation, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreationExecutor<TCandidate> Resolve(IOperator<Creation, TCandidate> source) =>
            (ICreationExecutor<TCandidate>)scope.ResolveOperator<Creation, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public ICreationExecutor<TCandidate>? ResolveOptional(IOperator<Creation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Creation, TCandidate> source,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public ICreationExecutor<TCandidate> Resolve(IOperator<Creation, TCandidate> source) =>
            (ICreationExecutor<TCandidate>)scope.ResolveOperator<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public ICreationExecutor<TCandidate>? ResolveOptional(IOperator<Creation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Creation, TCandidate> source,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
