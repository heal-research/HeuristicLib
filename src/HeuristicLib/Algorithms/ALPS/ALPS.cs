using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
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
