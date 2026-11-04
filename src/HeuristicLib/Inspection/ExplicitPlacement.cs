using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

// The explicit placement wrappers apply the advice an aspect would apply at their child, the way resolution applies it.
internal static class ExplicitPlacement
{
    public static IExecutor Apply<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope, IOperator<TOperation, TCandidate> child,
        OperatorAdvice<TOperation, TCandidate> advice)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        var operation = OperationInfo.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>();
        advice.Check<TSearchSpace, TProblem, TSearchState>(operation, child);
        var executor = advice.Wrap<TSearchSpace, TProblem, TSearchState>(scope.ResolveOperator(child), operation, child);
        operation.RequireExecutor(executor, advice);
        return executor;
    }
}
