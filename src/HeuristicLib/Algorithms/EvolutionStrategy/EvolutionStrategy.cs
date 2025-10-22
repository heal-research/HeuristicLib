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
  IInterceptor<TGenotype, EvolutionStrategyIterationResult<TGenotype>, TEncoding, TProblem>? interceptor)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationResult<TGenotype>, EvolutionStrategyIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; } = populationSize;
  public int NoChildren { get; } = noChildren;
  public EvolutionStrategyType Strategy { get; } = strategy;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  //public ICrossover<TGenotype, TGenotypeSearchSpace>? Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public double InitialMutationStrength { get; } = initialMutationStrength;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;

  public override EvolutionStrategyIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, EvolutionStrategyIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var pop = Creator.Create(PopulationSize, random, searchSpace, problem);
      var fitnesses1 = problem.Evaluate(pop);
      return new EvolutionStrategyIterationResult<TGenotype>(Population.From(pop, fitnesses1), InitialMutationStrength);
    }

    var oldPopulation = previousIterationResult.Population.Solutions;
    var parents = Selector.Select(oldPopulation, problem.Objective, PopulationSize, random, problem.SearchSpace, problem).ToArray();
    var children = Mutator.Mutate(parents.Select(x => x.Genotype).ToArray(), random, searchSpace, problem);

    // ToDo: optional crossover
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();

    var fitnesses = problem.Evaluate(children);

    if (Mutator is IVariableStrengthMutator<TGenotype, TEncoding, TProblem> vm) {
      //adapt Mutation Strength based on 1/5th rule
      var successes = parents.Zip(fitnesses).Count(t => t.Item2.CompareTo(t.Item1.ObjectiveVector, problem.Objective) == DominanceRelation.Dominates);
      var successRate = successes / (double)parents.Length;
      double newMutationStrength = successRate switch {
        > 0.2 => previousIterationResult.MutationStrength * 1.5,
        < 0.2 => previousIterationResult.MutationStrength / 1.5,
        _ => previousIterationResult.MutationStrength
      };
      vm.MutationStrength = newMutationStrength;
    }

    var population = Population.From(children, fitnesses);
    Replacer<TGenotype> replacer = Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer<TGenotype>(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer<TGenotype>(),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };
    var newPopulation = replacer.Replace(oldPopulation, population.Solutions, problem.Objective, random);
    return new EvolutionStrategyIterationResult<TGenotype>(Population.From(newPopulation), newMutationStrength);
  }

  protected override PopulationResult<TGenotype> FinalizeResult(EvolutionStrategyIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new PopulationResult<TGenotype>(iterationResult.Population);
  }
}
