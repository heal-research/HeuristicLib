using HEAL.HeuristicLib.Execution;
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
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, NSGA2<TGenotype, TSearchSpace, TProblem>.ExecutionState>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public new sealed class ExecutionState
    : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, ExecutionState>.ExecutionState
  {
    public required ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
    public required IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }
  }

  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator),
      Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
      Creator = resolver.Resolve(Creator),
      Crossover = resolver.Resolve(Crossover),
      Mutator = resolver.Resolve(Mutator),
      Selector = resolver.Resolve(Selector),
      Replacer = resolver.Resolve(Replacer)
    };
  }

  protected override PopulationState<TGenotype> ExecuteStep(
    PopulationState<TGenotype>? previousState,
    ExecutionState executionState,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = executionState.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var offspringCount = PopulationSize;
    var parents = executionState.Selector.Select(previousState.Population.Solutions, problem.Objective, offspringCount * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
    var children = executionState.Crossover.Cross(parents, random, problem.SearchSpace, problem);
    var mutants = executionState.Mutator.Mutate(children, random, problem.SearchSpace, problem);
    var newPopulation = Population.From(mutants, executionState.Evaluator.Evaluate(mutants, random, problem.SearchSpace, problem));
    var nextPopulation = executionState.Replacer.Replace(previousState.Population.Solutions, newPopulation.Solutions, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

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
