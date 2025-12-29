using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace>(
  int populationSize,
  ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> creator,
  ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> crossover,
  IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> mutator,
  double mutationRate,
  ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> selector,
  IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> evaluator,
  int elites,
  int randomSeed,
  ITerminator<TGenotype, AlpsIterationResult<TGenotype>, TSearchSpace, IProblem<TGenotype, TSearchSpace>> terminator,
  IInterceptor<TGenotype, AlpsIterationResult<TGenotype>, TSearchSpace, IProblem<TGenotype, TSearchSpace>>? interceptor = null)
  : AlpsGeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor)
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public class AlpsGeneticAlgorithm<TGenotype>(
  int populationSize,
  ICreator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> creator,
  ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> crossover,
  IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> mutator,
  double mutationRate,
  ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> selector,
  IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> evaluator,
  int elites,
  int randomSeed,
  ITerminator<TGenotype, AlpsIterationResult<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> terminator,
  IInterceptor<TGenotype, AlpsIterationResult<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>? interceptor = null)
  : AlpsGeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor);
