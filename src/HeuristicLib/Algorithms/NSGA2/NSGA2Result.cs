using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class NSGA2Result<TGenotype>(Population<TGenotype> population) : PopulationResult<TGenotype>(population);
