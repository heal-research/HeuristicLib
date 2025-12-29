using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public class NSGA2<TGenotype, TSearchSpace, TProblem> : Algorithm<TGenotype, TSearchSpace, TProblem, PopulationIterationState<TGenotype>>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TGenotype : class {
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  public override PopulationIterationState<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, PopulationIterationState<TGenotype>? previousState, IRandomNumberGenerator random) {
    if (previousState == null) {
      var initialSolutions = Creator.Create(PopulationSize, random, searchSpace, problem);
      var initialFitnesses = Evaluator.Evaluate(initialSolutions, random, searchSpace, problem);
      return new PopulationIterationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses), 
        CurrentIteration = 0
      };
    }
    
    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var parents = Selector.Select(previousState.Population.Solutions, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var children = Crossover.Cross(parents, random, searchSpace, problem);
    var mutants = Mutator.Mutate(children, random, searchSpace, problem);
    var newPop = Population.From(mutants, Evaluator.Evaluate(mutants, random, searchSpace, problem));
    var nextPop = Replacer.Replace(previousState.Population.Solutions, newPop.Solutions, problem.Objective, random, searchSpace, problem);

    return new PopulationIterationState<TGenotype> {
      Population = Population.From(nextPop),
      CurrentIteration = previousState.CurrentIteration + 1
    };
  }
}

public static class NSGA2 {
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
