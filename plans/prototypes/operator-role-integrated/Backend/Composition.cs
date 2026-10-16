using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorRolePrototype;

public sealed record Named<TCandidate, TOperation>(IOperator<TCandidate, TOperation> ChildOperator, string Name)
    : IOperator<TCandidate, TOperation>, INamedConfiguration
    where TOperation : IOperation<TOperation>
{
    public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
    public PreparedOperator Prepare() => new Prepared(ChildOperator);

    private sealed class Prepared(IOperator<TCandidate, TOperation> child) : PreparedOperator
    {
        public override PreparedFactory Bind<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationBinding<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState> binding)
        {
            if (child is not IOperator<TBoundCandidate, TBoundOperation> typed)
                throw new InvalidOperationException($"Named {typeof(TOperation)} cannot bind {typeof(TBoundOperation)}.");
            return binding.Forward(typed);
        }
    }
}

public static class OperatorCompositionExtensions
{
    public static Named<TCandidate, TOperation> Named<TCandidate, TOperation>(
        this IOperator<TCandidate, TOperation> source, string name)
        where TOperation : IOperation<TOperation> => new(source, name);
}
