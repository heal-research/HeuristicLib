using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public interface IInterceptor<TGenotype, TIterationResult, in TEncoding, in TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}
