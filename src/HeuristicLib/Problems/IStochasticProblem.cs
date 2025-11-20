using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<in TISolution, out TEncoding> : IProblem<TISolution, TEncoding> where TEncoding : class, IEncoding<TISolution>;
