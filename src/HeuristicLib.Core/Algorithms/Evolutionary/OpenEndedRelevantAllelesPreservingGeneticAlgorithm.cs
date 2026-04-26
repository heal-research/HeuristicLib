using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public new sealed class ExecutionState
    : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, ExecutionState>.ExecutionState
  {
    public required ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  }

  private double Strictness { get; } = 1.0;

  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public int Elites { get; init; } = 1;
  public required int MaxEffort { get; init; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator),
      Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
      Creator = resolver.Resolve(Creator),
      Crossover = resolver.Resolve(Crossover),
      Mutator = resolver.Resolve(Mutator),
      Selector = resolver.Resolve(Selector)
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

    var oldPopulation = previousState.Population.Solutions;

    IReadOnlyList<ISolution<TGenotype>> newPop;
    if (oldPopulation.Length <= 0) {
      var initialSolutions = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = executionState.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      newPop = Population.From(initialSolutions, initialFitnesses).Solutions;
    } else {
      var selected = executionState.Selector.Select(oldPopulation, problem.Objective, MaxEffort * 2, random, problem.SearchSpace, problem);
      var population = executionState.Crossover.Cross(selected.ToParents(problem.Objective), random, problem.SearchSpace, problem);
      population = executionState.Mutator.Mutate(population, random, problem.SearchSpace, problem);
      var fitnesses = executionState.Evaluator.Evaluate(population, random, problem.SearchSpace, problem);

      newPop = Population
               .From(population, fitnesses)
               .Solutions
               .Zip(selected.ToSolutionPairs())
               .Where(f => f.Item1.ObjectiveVector.Dominates(Combine(f.Item2, problem.Objective, Strictness), problem.Objective))
               .Select(f => f.Item1)
               .ToArray();
    }

    var targetPopsize = Elites + newPop.Count;
    var newPopulation = ElitismReplacer<TGenotype>.Replace(oldPopulation, newPop, problem.Objective, targetPopsize, Elites);

    return new PopulationState<TGenotype> {
      Population = Population.From(newPopulation)
    };
  }

  private static ObjectiveVector Combine((ISolution<TGenotype>, ISolution<TGenotype>) parents, Objective problemObjective, double strictness = 1.0)
  {
    var o1 = parents.Item1.ObjectiveVector;
    var o2 = parents.Item2.ObjectiveVector;
    if (o2.Dominates(o1, problemObjective)) {
      (o1, o2) = (o2, o1);
    }

    return strictness switch {
      >= 1.0 => o1,
      <= 0.0 => o2,
      _ => new ObjectiveVector(o1.Zip(o2).Select(pair => pair.Item1 * strictness + pair.Item2 * (1.0 - strictness)).ToArray())
    };
  }
}

// ReSharper disable once IdentifierTypo
public record OerapgaBuildBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TG, TS, TP>>,
    IBuilderWithCreator<TG, TS, TP>,
    IBuilderWithSelector<TG, TS, TP>,
    IBuilderWithCrossover<TG, TS, TP>,
    IBuilderWithMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public double MutationRate { get; set; } = 0.05;
  public int Elites { get; set; } = 1;
  public required int MaxEffort { get; set; }
  public required ICreator<TG, TS, TP> Creator { get; set; }
  public required ICrossover<TG, TS, TP> Crossover { get; set; }
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public int PopulationSize { get; set; } = 100;
  public ISelector<TG, TS, TP> Selector { get; set; } = new TournamentSelector<TG>(2);

  public override OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TG, TS, TP> Build()
  {
    return new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TG, TS, TP>() {
      PopulationSize = PopulationSize,
      Creator = Creator,
      Crossover = Crossover,
      Selector = Selector,
      Evaluator = Evaluator,
      Interceptor = Interceptor,
      Mutator = Mutator.WithRate(MutationRate),
      MaxEffort = MaxEffort
    };
  }
}
