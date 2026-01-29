using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<in TISolution, out TEncoding> : IProblem<TISolution, TEncoding> where TEncoding : class, IEncoding<TISolution> {
  IRandomNumberGenerator EnvironmentRandom { init; }
};
