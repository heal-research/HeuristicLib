using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator : IExecutionInstanceResolvable;

public interface IOperatorInstance : IExecutionInstance;

/// <summary>
/// Declares that a value recommends an operator for a specific operator role.
/// </summary>
/// <remarks>
/// A successful call must return <see langword="true"/> and a new, nonnull operator. Observation sources are matched
/// by reference, so a shared instance would couple configurations that accepted the same recommendation. Returning
/// <see langword="false"/> with a null recommendation declines to recommend an operator for the source's current state.
/// </remarks>
public interface IRecommends<TOperator>
    where TOperator : class, IOperator
{
    bool TryCreateRecommendedOperator([NotNullWhen(true)] out TOperator? recommendation);
}
