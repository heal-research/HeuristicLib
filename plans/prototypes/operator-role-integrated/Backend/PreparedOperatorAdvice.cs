using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public interface ISuccessHook
{
    void Success(int candidateWork);
}

public abstract class PreparedOperatorAdvice
{
    public abstract PreparedWrapper Bind<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
        IOperator<TCandidate, TOperation> source,
        OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class PreparedOperatorAdvice<TCandidate, TOperation> where TOperation : IOperation<TOperation>
{
    public abstract PreparedWrapper Bind<TSearchSpace, TProblem, TRunSearchState>(
        IOperator<TCandidate, TOperation> source,
        OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
