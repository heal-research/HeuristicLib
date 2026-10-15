using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// A multi crossover owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiCrossover<TCandidate>
    : ICrossover<TCandidate>
{
    protected MultiCrossover(IReadOnlyList<ICrossover<TCandidate>> childCrossovers)
    {
        ChildCrossovers = childCrossovers.ToValueArray();
    }

    public ValueArray<ICrossover<TCandidate>> ChildCrossovers { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildCrossovers]);

    /// <summary>Prepares this operator once, then binds its children in each construction scope.</summary>
    public ExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateCompositeFactory<TRunSearchSpace, TRunProblem>();
        return scope =>
        {
            var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
            return create([.. ChildCrossovers.Select(child => typed.Resolve(child))]);
        };
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved children.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract CompositeExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiCrossoverExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> childCrossovers)
    : CrossoverExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> ChildCrossovers { get; } = childCrossovers;
}
