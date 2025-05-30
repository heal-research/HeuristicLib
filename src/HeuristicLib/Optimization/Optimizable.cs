namespace HEAL.HeuristicLib.Optimization;

public interface IOptimizable<in TSolution, out TSearchSpace>
  where TSearchSpace : ISearchSpace<TSolution>
{
  Fitness Evaluate(TSolution solution);
  Objective Objective { get; }
  TSearchSpace SearchSpace { get; }
}
