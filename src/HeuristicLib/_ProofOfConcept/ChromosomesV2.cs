namespace HEAL.HeuristicLib._ProofOfConcept.ChromosomesV2;

public interface IGenotype {}

public interface IChromosome : IGenotype { }

public class Permutation : IChromosome { }
public class RealVector : IChromosome { }
public class Tree : IChromosome { }

public record MultiGenotype(RealVector Parameters, Tree Tree) : IGenotype;

public class MultiGenotypeCreator(ICreator<RealVector> realVectorCreator, ICreator<Tree> treeCreator) : ICreator<MultiGenotype> {
  public MultiGenotype Create() {
    return new MultiGenotype(realVectorCreator.Create(), treeCreator.Create());
  }
}

public class MultiGenotypeCrossover(ICrossover<RealVector> realVectorCrossover, ICrossover<Tree> treeCrossover) : ICrossover<MultiGenotype> {
  public MultiGenotype Crossover(MultiGenotype parent1, MultiGenotype parent2) {
    return new MultiGenotype(realVectorCrossover.Crossover(parent1.Parameters, parent2.Parameters), treeCrossover.Crossover(parent1.Tree, parent2.Tree));
  }
}

public interface IObjective { }

public record Phenotype<TGenotype, TObjective>(TGenotype Genotype, TObjective Objective) 
  where TGenotype : IGenotype where TObjective : IObjective;


public interface ICreator<out TGenotype> where TGenotype : IGenotype { 
  TGenotype Create();
}

public interface ICrossover<TGenotype> where TGenotype : IGenotype { 
  TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}


public interface IMutator<TGenotype> where TGenotype : IGenotype { 
  TGenotype Mutate(TGenotype genotype);
}
public interface IEvaluator<TGenotype, TObjective> where TGenotype : IGenotype where TObjective : IObjective { 
  Phenotype<TGenotype, TObjective>[] Evaluate(TGenotype[] genotype);
}
public interface ISelector<TGenotype, TObjective> where TGenotype : IGenotype where TObjective : IObjective { 
  TGenotype[] Select(Phenotype<TGenotype, TObjective>[] population, int count);
}

public class PermutationCreator : ICreator<Permutation> { public Permutation Create() { throw new NotImplementedException(); } }
public class OrderCrossover : ICrossover<Permutation> { public Permutation Crossover(Permutation parent1, Permutation parent2) { throw new NotImplementedException(); } }
public class PermutationMutator : IMutator<Permutation> { public Permutation Mutate(Permutation genotype) { throw new NotImplementedException(); } }

public class RealVectorCreator : ICreator<RealVector> { public RealVector Create() { throw new NotImplementedException(); } }
public class RealVectorCrossover : ICrossover<RealVector> { public RealVector Crossover(RealVector parent1, RealVector parent2) { throw new NotImplementedException(); } }
public class RealVectorMutator : IMutator<RealVector> { public RealVector Mutate(RealVector genotype) { throw new NotImplementedException(); } }

public class TreeCreator : ICreator<Tree> { public Tree Create() { throw new NotImplementedException(); } }
public class TreeCrossover : ICrossover<Tree> { public Tree Crossover(Tree parent1, Tree parent2) { throw new NotImplementedException(); } }
public class TreeMutator : IMutator<Tree> { public Tree Mutate(Tree genotype) { throw new NotImplementedException(); } }

public readonly record struct ObjectiveValue(double Value) : IObjective;

public class GeneticAlgorithm<TGenotype> where TGenotype : IGenotype {
  public GeneticAlgorithm(int populationSize, ICreator<TGenotype> creator, IEvaluator<TGenotype, ObjectiveValue> evaluator, ICrossover<TGenotype> crossover) {}
}


#pragma warning disable S125
// public static class Test {
//   private class PermutationEvaluator : IEvaluator<Permutation, ObjectiveValue> { public Phenotype<Permutation, ObjectiveValue>[] Evaluate(Permutation[] genotype) { throw new NotImplementedException(); } }
//   private class MultiGenotypeEvaluator : IEvaluator<MultiGenotype, ObjectiveValue> { public Phenotype<MultiGenotype, ObjectiveValue>[] Evaluate(MultiGenotype[] genotype) { throw new NotImplementedException(); } }
//   private static void TestWithPermutation() {
//     var ga = new GeneticAlgorithm<Permutation>(10, new PermutationCreator(), new PermutationEvaluator(), new OrderCrossover());
//   }
//   private static void TestWithMultiGenotype() {
//     var ga = new GeneticAlgorithm<MultiGenotype>(
//     10, 
//     new MultiGenotypeCreator(new RealVectorCreator(), new TreeCreator()), 
//     new MultiGenotypeEvaluator(), 
//     new MultiGenotypeCrossover(new RealVectorCrossover(), new TreeCrossover()));
//   }
// }
#pragma warning restore S125
