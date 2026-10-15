using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// A multi replacer owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiReplacer<TCandidate>
    : IReplacer<TCandidate>
{
    protected MultiReplacer(IReadOnlyList<IReplacer<TCandidate>> childReplacers)
    {
        ChildReplacers = childReplacers.ToValueArray();
    }

    public ValueArray<IReplacer<TCandidate>> ChildReplacers { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildReplacers]);

    /// <summary>Prepares this operator once, then binds its children in each construction scope.</summary>
    public ExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateCompositeFactory<TRunSearchSpace, TRunProblem>();
        return scope =>
        {
            var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
            return create([.. ChildReplacers.Select(child => typed.Resolve(child))]);
        };
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved children.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract CompositeExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiReplacerExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IReplacerExecution<TCandidate, TSearchSpace, TProblem>> childReplacers)
    : ReplacerExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IReplacerExecution<TCandidate, TSearchSpace, TProblem>> ChildReplacers { get; } = childReplacers;
}
