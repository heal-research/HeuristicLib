namespace HEAL.HeuristicLib;

public interface IOptimizable<in TGenotype, out TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  Fitness Evaluate(TGenotype genotype);
  Objective Objective { get; }
  TSearchSpace SearchSpace { get; }
}
