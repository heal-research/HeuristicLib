using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.RealVectorOperators.Mutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public class EvolutionStrategyIterationResult<TGenotype>(Population<TGenotype> population, double mutationStrength) : PopulationIterationResult<TGenotype>(population) {
  public double MutationStrength { get; } = mutationStrength;
}

public class EvolutionStrategy<TGenotype, TEncoding, TProblem>(
  int populationSize,
  int noChildren,
  EvolutionStrategyType strategy,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  double initialMutationStrength,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  int? randomSeed,
  ITerminator<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  ICrossover<TGenotype, TEncoding, TProblem>? crossover = null,
  IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationResult<TGenotype>, EvolutionStrategyIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; } = populationSize;
  public int NoChildren { get; } = noChildren;
  public EvolutionStrategyType Strategy { get; } = strategy;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; } = crossover;
  public double InitialMutationStrength { get; } = initialMutationStrength;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;

  public override EvolutionStrategyIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, EvolutionStrategyIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var pop = Creator.Create(PopulationSize, random, searchSpace, problem);
      var fitnesses1 = problem.Evaluate(pop);
      return new EvolutionStrategyIterationResult<TGenotype>(Population.From(pop, fitnesses1), InitialMutationStrength);
    }

    IReadOnlyList<TGenotype> parents;
    IReadOnlyList<ObjectiveVector> parentQualities;

    if (Crossover == null) {
      var parentSolutions = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);
      parents = parentSolutions.Select(x => x.Genotype).ToArray();
      parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
    } else {
      var parentSolutions = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, PopulationSize * 2, random, problem.SearchSpace, problem);
      parents = Crossover!.Cross(parentSolutions.ToGenotypePairs(), random, searchSpace, problem);
      parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
    }

    var children = Mutator.Mutate(parents, random, searchSpace, problem);
    var fitnesses = problem.Evaluate(children);

    double newMutationStrength = previousIterationResult.MutationStrength;
    if (Mutator is IVariableStrengthMutator<TGenotype, TEncoding, TProblem> vm) {
      //adapt Mutation Strength based on 1/5th rule
      var successes = parentQualities.Zip(fitnesses).Count(t => t.Item2.CompareTo(t.Item1, problem.Objective) == DominanceRelation.Dominates);
      var successRate = successes / (double)PopulationSize;
      newMutationStrength *= successRate switch {
        > 0.2 => 1.5,
        < 0.2 => 1 / 1.5,
        _ => 1
      };
      vm.MutationStrength = newMutationStrength;
    }

    var population = Population.From(children, fitnesses);
    Replacer<TGenotype> replacer = Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer<TGenotype>(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer<TGenotype>(),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };
    var newPopulation = replacer.Replace(previousIterationResult.Population.Solutions, population.Solutions, problem.Objective, random);
    return new EvolutionStrategyIterationResult<TGenotype>(Population.From(newPopulation), newMutationStrength);
  }

  protected override PopulationResult<TGenotype> FinalizeResult(EvolutionStrategyIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new PopulationResult<TGenotype>(iterationResult.Population);
  }
}
