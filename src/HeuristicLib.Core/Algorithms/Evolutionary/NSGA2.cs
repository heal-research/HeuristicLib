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
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, NSGA2<TGenotype, TSearchSpace, TProblem>.State>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  public class State : IterativeAlgorithmState
  {
    public ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; }
    public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover { get; }
    public IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; }
    public ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; }
    public IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer { get; }

    public State(ExecutionInstanceRegistry instanceRegistry, NSGA2<TGenotype, TSearchSpace, TProblem> algorithm) : base(instanceRegistry, algorithm)
    {
      Creator = instanceRegistry.Resolve(algorithm.Creator);
      Crossover = instanceRegistry.Resolve(algorithm.Crossover);
      Mutator = instanceRegistry.Resolve(algorithm.Mutator);
      Selector = instanceRegistry.Resolve(algorithm.Selector);
      Replacer = instanceRegistry.Resolve(algorithm.Replacer);
    }
  }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new(instanceRegistry, this);

  protected override PopulationState<TGenotype> ExecuteStep(State state, PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = state.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = state.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var parents = state.Selector.Select(previousState.Population.Solutions, problem.Objective, PopulationSize * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
    var children = state.Crossover.Cross(parents, random, problem.SearchSpace, problem);
    var mutants = state.Mutator.Mutate(children, random, problem.SearchSpace, problem);
    var newPop = Population.From(mutants, state.Evaluator.Evaluate(mutants, random, problem.SearchSpace, problem));
    var nextPop = state.Replacer.Replace(previousState.Population.Solutions, newPop.Solutions, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

    return new PopulationState<TGenotype> {
      Population = Population.From(nextPop)
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
