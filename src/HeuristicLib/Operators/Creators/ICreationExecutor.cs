using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ICreationExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class CreationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Creation, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Creation, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Creation, TCandidate> source,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Creation, TCandidate> source) =>
            (ICreationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Creation, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Creation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ICreationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Creation, TCandidate, TSearchSpace, TProblem, ISearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Creation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Creation, TCandidate> source,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Creation, TCandidate> source) =>
            (ICreationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Creation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ICreationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Creation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Creation, TCandidate> source,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
