using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

public sealed record Named<TCandidate, TOperation>(IOperator<TCandidate, TOperation> ChildOperator, string Name)
    : IOperator<TCandidate, TOperation>, INamedConfiguration
    where TOperation : IOperationDescriptor<TOperation>
{
    public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
    public OperatorExecutorFactory CreateExecutorFactory() => new Factory(ChildOperator);

    private sealed class Factory(IOperator<TCandidate, TOperation> child) : OperatorExecutorFactory
    {
        public override ExecutorFactory GetExecutorFactory<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState> adapter)
        {
            if (child is not IOperator<TBoundCandidate, TBoundOperation> typed)
                throw new InvalidOperationException($"Named {typeof(TOperation)} cannot bind {typeof(TBoundOperation)}.");
            return adapter.Forward(typed);
        }
    }
}

public static class OperatorCompositionExtensions
{
    public static Named<TCandidate, TOperation> Named<TCandidate, TOperation>(
        this IOperator<TCandidate, TOperation> source, string name)
        where TOperation : IOperationDescriptor<TOperation> => new(source, name);
}
