using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TCandidate, TSearchState>
    : IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    IOperator<Interception<TSearchState>, TCandidate>? Interceptor { get; }
}
