using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public partial class ResolutionScope
{
    /// <summary>Builds an algorithm's executor for the run's types, or returns the one this scope already built for it.</summary>
    /// <remarks>
    /// The algorithm and its declared children must support the run, and the algorithm must produce the requested search state. A
    /// mismatch names every configuration responsible, such as an operator written for another search space. Direct
    /// execution through a scope leaves <see cref="RunResults.Elapsed"/> at zero; <c>AlgorithmRun</c> owns elapsed-time
    /// measurement. Use <c>algorithm.Timed(duration)</c> to measure direct scope execution explicitly.
    /// </remarks>
    public IAlgorithmExecutor<TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> source, TProblem problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        node.Tree.ThrowIfFailed();
        var algorithm = SearchConfigurationValidation.RequireSupport<TCandidate, TSearchState>(source, ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>());
        using var building = node.Tree.Enter(algorithm);
        var owner = Owner(algorithm);
        var existing = Existing(owner, algorithm, static executor => executor is IAlgorithmExecutor<TSearchState>, problem);
        node.MarkResolved(algorithm);
        try
        {
            // The resolver is the one place that constructs executors; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            return (IAlgorithmExecutor<TSearchState>?)existing
                ?? Build(owner, algorithm, scope => algorithm.CreateExecutor(scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>(problem)), problem);
#pragma warning restore HLib0001
        }
        catch
        {
            node.Tree.Fail(algorithm);
            throw;
        }
    }

    [return: NotNullIfNotNull(nameof(algorithm))]
    public IAlgorithmExecutor<TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate>? algorithm, TProblem problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        algorithm is null ? null : Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, problem);

    /// <inheritdoc cref="TryResolve{TExecutor}"/>
    public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> algorithm, TProblem problem,
        [NotNullWhen(true)] out IAlgorithmExecutor<TSearchState>? executor, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Attempt(() => Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, problem), out executor, out reason);
}
