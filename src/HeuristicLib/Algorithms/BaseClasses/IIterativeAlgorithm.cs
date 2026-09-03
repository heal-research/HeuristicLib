using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TCandidate>
  : IAlgorithm<TCandidate>
{
    IInterceptor<TCandidate>? Interceptor { get; }
}
