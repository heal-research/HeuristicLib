using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IDynamicProblem<in TGenotype, out TEncoding> : IProblem<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>;
