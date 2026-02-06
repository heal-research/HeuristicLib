using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public class NSGA2<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TGenotype : class
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  public override NSGA2Instance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var creatorInstance = instanceRegistry.GetOrAdd(Creator, () => Creator.CreateExecutionInstance(instanceRegistry));
    var evaluatorInstance = instanceRegistry.GetOrAdd(Evaluator, () => Evaluator.CreateExecutionInstance(instanceRegistry));
    
    return new NSGA2Instance<TGenotype, TSearchSpace, TProblem>(
      Interceptor,
      evaluatorInstance,
      PopulationSize,
      creatorInstance,
      Crossover,
      Mutator,
      Selector,
      Replacer
    );
  }
}

public class NSGA2Instance<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TGenotype : class
{
  protected readonly int PopulationSize;
  protected readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator;
  protected readonly ICrossover<TGenotype, TSearchSpace, TProblem> Crossover;
  protected readonly IMutator<TGenotype, TSearchSpace, TProblem> Mutator;
  protected readonly ISelector<TGenotype, TSearchSpace, TProblem> Selector;
  protected readonly IReplacer<TGenotype, TSearchSpace, TProblem> Replacer;


  public NSGA2Instance(IInterceptor<TGenotype, PopulationState<TGenotype>, TSearchSpace, TProblem>? interceptor, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, int populationSize, ICreatorInstance<TGenotype, TSearchSpace, TProblem> creator, ICrossover<TGenotype, TSearchSpace, TProblem> crossover, IMutator<TGenotype, TSearchSpace, TProblem> mutator, ISelector<TGenotype, TSearchSpace, TProblem> selector, IReplacer<TGenotype, TSearchSpace, TProblem> replacer) 
    : base(interceptor, evaluator)
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    Selector = selector;
    Replacer = replacer;
  }
  
  public override PopulationState<TGenotype> ExecuteStep(PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses),
        //CurrentIteration = 0
      };
    }

    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var parents = Selector.Select(previousState.Population.Solutions, problem.Objective, offspringCount * 2, random, problem.SearchSpace, problem).ToGenotypePairs();
    var children = Crossover.Cross(parents, random, problem.SearchSpace, problem);
    var mutants = Mutator.Mutate(children, random, problem.SearchSpace, problem);
    var newPop = Population.From(mutants, Evaluator.Evaluate(mutants, random, problem.SearchSpace, problem));
    var nextPop = Replacer.Replace(previousState.Population.Solutions, newPop.Solutions, problem.Objective, random, problem.SearchSpace, problem);

    return new PopulationState<TGenotype> {
      Population = Population.From(nextPop),
      //CurrentIteration = previousState.CurrentIteration + 1
    };
  }
}

public static class NSGA2
{
  public static NSGA2Builder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator, bool dominateOnEquals = true)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> where TGenotype : class => new() {
    Mutator = mutator,
    Crossover = crossover,
    Creator = creator,
    Selector = new ParetoCrowdingTournamentSelector<TGenotype>(dominateOnEquals)
  };
}
