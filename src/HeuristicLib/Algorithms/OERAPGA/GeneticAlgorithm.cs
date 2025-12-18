using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms.OERAPGA;

public static class OpenEndedRelevantAllelesPreservingGeneticAlgorithm {
  public static OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TEncoding, TProblem>.Builder GetBuilder<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> where TGenotype : class => new() {
    Mutator = mutator,
    Crossover = crossover,
    Creator = creator,
    MaxEffort = 200
  };
}
