using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public static class AlgorithmFactory {
  public static GeneticAlgorithm<TGenotype, TEncoding, TProblem> GeneticAlgorithm<TGenotype, TEncoding, TProblem>(
    int populationSize,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null
  ) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> => new(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor);
}
