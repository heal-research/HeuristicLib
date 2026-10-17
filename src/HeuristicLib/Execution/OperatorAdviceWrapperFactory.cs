using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Receives a notification after an operation returns successfully, including its candidate-work count.</summary>
public interface IOperationSuccessCallback
{
    void OnSucceeded(int candidateWork);
}

/// <summary>
/// Provides functions that wrap an executor with advice. Keeps data, such as a call counter, for one advice instance
/// and one selected operator configuration when executors are constructed again.
/// </summary>
public abstract class OperatorAdviceWrapperFactory
{
    public abstract WrapperExecutorFactory GetWrapperFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        IOperator<TOperation, TCandidate> source,
        OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}

/// <summary>Provides advice wrappers for one selected operator and operation, keeping advice data when wrappers are rebuilt.</summary>
public abstract class OperatorAdviceWrapperFactory<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
{
    public abstract WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TRunSearchState>(
        IOperator<TOperation, TCandidate> source,
        OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
