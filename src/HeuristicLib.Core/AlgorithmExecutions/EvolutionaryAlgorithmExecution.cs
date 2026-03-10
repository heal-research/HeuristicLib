using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutions;

public class EvolutionaryAlgorithmExecution<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected readonly int PopulationSize;
  protected readonly int OffspringSize;
  protected readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator;
  protected readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector;
  protected readonly IVariationInstance<TGenotype, TSearchSpace, TProblem> Variator;
  protected readonly IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer;

  public EvolutionaryAlgorithmExecution(
    int populationSize,
    int offspringSize,
    ICreatorInstance<TGenotype, TSearchSpace, TProblem> creator,
    ISelectorInstance<TGenotype, TSearchSpace, TProblem> selector,
    IVariationInstance<TGenotype, TSearchSpace, TProblem> variator,
    IReplacerInstance<TGenotype, TSearchSpace, TProblem> replacer,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator,
    IInterceptorInstance<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>? interceptor
  ) : base(interceptor, evaluator)
  {
    PopulationSize = populationSize;
    OffspringSize = offspringSize;
    Selector = selector;
    Creator = creator;
    Variator = variator;
    Replacer = replacer;
  }

  public override PopulationState<TGenotype> ExecuteStep(PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var oldPopulation = previousState.Population.Solutions;

    var parents = Selector.Select(oldPopulation, problem.Objective, OffspringSize, random, problem.SearchSpace, problem)
                          .Select(x => x.Genotype).ToList();

    var offspring = Variator.Alter(parents, random, problem.SearchSpace, problem);

    var fitnesses = Evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
    var offspringPopulation = Population.From(offspring, fitnesses).Solutions;

    var newPopulation = Replacer.Replace(oldPopulation, offspringPopulation, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

    return new PopulationState<TGenotype> {
      Population = Population.From(newPopulation)
    };
  }
}
