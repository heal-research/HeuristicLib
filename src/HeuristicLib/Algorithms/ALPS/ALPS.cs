using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class ALPSGeneticAlgorithm<TGenotype, TEncoding>(
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
  : ALPSGeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>;

public class AlpsGeneticAlgorithm<TGenotype>(
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
  : ALPSGeneticAlgorithm<TGenotype, IEncoding<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor);
