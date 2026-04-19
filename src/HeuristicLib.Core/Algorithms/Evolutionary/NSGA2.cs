using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

#pragma warning disable S101
public record NSGA2<TGenotype, TSearchSpace, TProblem>
#pragma warning restore S101
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  protected override PopulationState<TGenotype> ExecuteStep(
    PopulationState<TGenotype>? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = executor.Create(Creator, PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = executor.Evaluate(Evaluator, initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var offspringCount = PopulationSize;
    var parents = executor.Select(Selector, previousState.Population.Solutions, problem.Objective, offspringCount * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
    var children = executor.Cross(Crossover, parents, random, problem.SearchSpace, problem);
    var mutants = executor.Mutate(Mutator, children, random, problem.SearchSpace, problem);
    var newPopulation = Population.From(mutants, executor.Evaluate(Evaluator, mutants, random, problem.SearchSpace, problem));
    var nextPopulation = executor.Replace(Replacer, previousState.Population.Solutions, newPopulation.Solutions, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

    return new PopulationState<TGenotype> {
      Population = Population.From(nextPopulation)
    };
  }
}

#pragma warning disable S101
public static class NSGA2
#pragma warning restore S101
{
  public static NSGA2Builder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator, bool dominateOnEquals = true)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new NSGA2Builder<TGenotype, TSearchSpace, TProblem> {
      Mutator = mutator,
      Crossover = crossover,
      Creator = creator,
      Selector = new ParetoCrowdingTournamentSelector<TGenotype>(dominateOnEquals)
    };
  }
}
