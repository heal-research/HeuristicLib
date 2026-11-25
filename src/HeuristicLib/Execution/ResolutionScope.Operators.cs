using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public partial class ResolutionScope
{
    /// <summary>Resolves an operator's executor for one operation and the run's types.</summary>
    /// <remarks>The result implements the operation's executor interface; operation-specific resolution extensions cast it.</remarks>
    public IExecutor ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<TOperation, TCandidate> source, TProblem problem)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        node.Tree.ThrowIfFailed();
        SearchConfigurationValidation.RequireSupport(source, ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>());
        using var building = node.Tree.Enter(source);
        var owner = Owner(source);
        var existing = Existing(owner, source, static executionNode => executionNode is IExecutor executor && TOperation.IsExecutor<TCandidate, TSearchState>(executor), problem);
        node.MarkResolved(source);
        try
        {
            if (existing is IExecutor resolved)
                return resolved;
            // The resolver is the one place that constructs executors; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            var executor = Build(owner, source, scope => source.CreateExecutor(scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>(problem)), problem);
#pragma warning restore HLib0001
            OperationInfo.RequireExecutor<TOperation, TCandidate, TSearchState>(executor, source);
            return executor;
        }
        catch
        {
            node.Tree.Fail(source);
            throw;
        }
    }

    /// <summary>Resolves an operator like <c>ResolveOperator</c> and returns its executor as the operation's interface.</summary>
    /// <remarks>
    /// A rejection before anything is built becomes a reason and leaves the scope usable; a construction failure ends
    /// the scope tree and propagates. <typeparamref name="TExecutor"/> is the operation's executor interface for the
    /// run types.
    /// </remarks>
    public bool TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor>(IOperator<TOperation, TCandidate> source, TProblem problem,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TExecutor : class, IExecutor =>
        Attempt(() => (TExecutor)ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(source, problem), out executor, out reason);
}
