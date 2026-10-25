using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public partial class ResolutionScope
{
    /// <summary>Resolves an operator's executor for one operation and the run's types, applying matching advice.</summary>
    /// <remarks>
    /// The result implements the operation's executor interface; operation-specific resolution extensions cast it.
    /// Every matching advice checks the operator before its executor is built.
    /// </remarks>
    public IExecutor ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        if (!source.Fits(ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>()))
            throw ExecutionSignature.Mismatch(source,
                source is Operator<TOperation, TCandidate> authored ? authored.SupportedExecutionDescription : "its declared signature",
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)));
        if (!state.Constructing.Add(source))
            throw new InvalidOperationException($"Recursive construction of {source.GetType().Name} through {typeof(TOperation).Name}.");
        try
        {
            var operation = OperationInfo.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>();
            var owner = state.GetOrAddOwner(source);

            List<Advice>? applicable = null;
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(source) && CheckOperatorAdvice<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(advice, operation, source))
                    (applicable ??= []).Add(advice);

            var executor = source.CreateExecutor(WithState(owner).For<TCandidate, TSearchSpace, TProblem, TSearchState>());
            operation.RequireExecutor(executor, source);
            if (applicable is not null)
                foreach (var advice in applicable)
                {
                    executor = advice is OperatorAdvice general
                        ? general.Wrap(executor, operation, source)
                        : ((OperatorAdvice<TOperation, TCandidate>)advice).Wrap<TSearchSpace, TProblem, TSearchState>(executor, operation, source);
                    operation.RequireExecutor(executor, advice);
                }
            return executor;
        }
        finally
        {
            state.Constructing.Remove(source);
        }
    }

    /// <summary>Resolves an operator like <c>ResolveOperator</c> and returns its executor as the operation's interface.</summary>
    /// <remarks>
    /// Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.
    /// <typeparamref name="TExecutor"/> is the operation's executor interface for the run types.
    /// </remarks>
    public bool TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor>(IOperator<TOperation, TCandidate> source,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TExecutor : class, IExecutor =>
        Attempt(() => (TExecutor)ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(source), out executor, out reason);

    // Returns whether the advice wraps this operation; advice for another operation of the same configuration is skipped.
    private static bool CheckOperatorAdvice<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(Advice advice,
        OperationInfo operation, IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        switch (advice)
        {
            case OperatorAdvice general:
                general.Check(operation, source);
                return true;
            case OperatorAdvice<TOperation, TCandidate> typed:
                typed.Check<TSearchSpace, TProblem, TSearchState>(operation, source);
                return true;
            case ITypedOperatorAdvice:
                return false;
            default:
                throw new InvalidOperationException($"{advice.GetType().FullName} cannot wrap operation {typeof(TOperation).Name} " +
                    $"on {source.GetType().Name}. Supported extension bases are OperatorAdvice and OperatorAdvice<TOperation, TCandidate>.");
        }
    }
}
