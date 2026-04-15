//using HEAL.HeuristicLib.Execution;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Problems;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;

//namespace HEAL.HeuristicLib.Operators.Variations;

// ToDo: Probably combine all 3 Variations in single one with Crossover/Mutator no-ops, just in case.
//public class CrossoverAndMutationVariation<TGenotype, TSearchSpace, TProblem>
//  : IVariation<TGenotype, TSearchSpace, TProblem>
//  where TSearchSpace : class, ISearchSpace<TGenotype>
//  where TProblem : class, IProblem<TGenotype, TSearchSpace>
//{
//  public ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
//  public IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }

//  public CrossoverAndMutationVariation(ICrossover<TGenotype, TSearchSpace, TProblem> crossover, IMutator<TGenotype, TSearchSpace, TProblem> mutator)
//  {
//    Crossover = crossover;
//    Mutator = mutator;
//  }

//  public IVariationInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
//  {
//    return new Instance(instanceRegistry.Resolve(Crossover), instanceRegistry.Resolve(Mutator));
//  }

//  public class Instance(ICrossoverInstance<TGenotype, TSearchSpace, TProblem> crossoverInstance, IMutatorInstance<TGenotype, TSearchSpace, TProblem> mutatorInstance) : IVariationInstance<TGenotype, TSearchSpace, TProblem>
//  {
//    public IReadOnlyList<TGenotype> Alter(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
//    {
//      if (parent.Count % 2 != 0)
//        throw new ArgumentException("Crossover requires an even number of parents.", nameof(parent));

//      var parentPairs = parent.ToParentPairs();
//      var offspring = crossoverInstance.Cross(parentPairs, random, searchSpace, problem);

//      return mutatorInstance.Mutate(offspring, random, searchSpace, problem);
//    }
//  }
//}


