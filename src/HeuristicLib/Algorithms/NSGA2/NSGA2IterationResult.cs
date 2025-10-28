using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class NSGA2IterationResult<TGenotype>(Population<TGenotype> population) : PopulationIterationResult<TGenotype>(population);
