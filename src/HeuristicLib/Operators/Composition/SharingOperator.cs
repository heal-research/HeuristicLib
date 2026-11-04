using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Runs its child operator with one shared executor for each of the given configurations.</summary>
/// <remarks>
/// Every use of a shared configuration inside the child receives the same executor, for as long as this operator's
/// executor lives. Constructing this operator again, for example in the next Cycle turn or another run, creates new
/// shared executors. Other configurations are constructed afresh for every use, as usual.
/// </remarks>
public sealed record SharingOperator<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    public SharingOperator(IOperator<TOperation, TCandidate> childOperator, IReadOnlyList<IConfigurationNode> shared)
        : base(childOperator)
    {
        if (shared.Count == 0)
            throw new ArgumentException("At least one configuration must be shared.", nameof(shared));
        Shared = shared.ToValueArray();
    }

    public ValueArray<IConfigurationNode> Shared { get; init; }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        scope.Sharing([.. Shared]).ResolveOperator(ChildOperator);
}

public static class SharingOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Shares one executor of each given configuration across every use inside this operator.</summary>
        public SharingOperator<TOperation, TCandidate> Sharing(params IReadOnlyList<IConfigurationNode> shared) => new(source, shared);
    }
}
