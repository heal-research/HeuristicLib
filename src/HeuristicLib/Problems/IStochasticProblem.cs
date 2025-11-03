using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<in TSolution, out TEncoding> : IProblem<TSolution, TEncoding> where TEncoding : class, IEncoding<TSolution>;
