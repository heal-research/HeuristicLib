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
    public IExecutor ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        node.Tree.ThrowIfFailed();
        var execution = ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>();
        if (!execution.Supports(source))
        {
            // When the operator itself is written for this run, a child is responsible, so the failure names the child.
            throw source.Supports(execution)
                ? SearchConfigurationValidation.Mismatch(source, execution)
                : ExecutionSignature.Mismatch(source,
                    source is Operator<TOperation, TCandidate> authored ? authored.SupportedExecutionDescription : "its declared signature",
                    ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)));
        }
        using var building = node.Tree.Enter(source);
        var owner = Owner(source);
        var existing = Existing(owner, source, TOperation.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>);
        node.MarkResolved(source);
        try
        {
            if (existing is not null)
                return existing;
            // The resolver is the one place that constructs executors; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            var executor = Build(owner, source, scope => source.CreateExecutor(scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>()));
#pragma warning restore HLib0001
            OperationInfo.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>().RequireExecutor(executor, source);
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
    public bool TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor>(IOperator<TOperation, TCandidate> source,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TExecutor : class, IExecutor =>
        Attempt(() => (TExecutor)ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(source), out executor, out reason);
}
