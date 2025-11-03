using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class GeneticAlgorithm<TGenotype, TEncoding>(
  int populationSize,
  ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator,
  ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover,
  IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator,
  double mutationRate,
  ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector,
  IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> evaluator,
  int elites,
  int randomSeed,
  ITerminator<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator,
  IInterceptor<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>;

public class GeneticAlgorithm<TGenotype>(
  int populationSize,
  ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> creator,
  ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> crossover,
  IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> mutator,
  double mutationRate,
  ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> selector,
  IEvaluator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> evaluator,
  int elites,
  int randomSeed,
  ITerminator<TGenotype, ALPSIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> terminator,
  IInterceptor<TGenotype, ALPSIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor);
