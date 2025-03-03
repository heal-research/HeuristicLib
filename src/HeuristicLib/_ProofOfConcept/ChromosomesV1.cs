namespace HEAL.HeuristicLib._ProofOfConcept.ChromosomesV1;


public interface IChromosome { }
public class Permutation : IChromosome { }
public class RealVector : IChromosome { }
public class Tree : IChromosome { }


public interface IGenotype : IChromosome {}
public record Genotype<TChromosome>(TChromosome Chromosome) : IGenotype;
public record MultiGenotype(RealVector Parameters, Tree Tree) : IGenotype;

public interface IObjective : IComparable<IObjective> { }

public record Phenotype<TGenotype, TObjective>(TGenotype Genotype, TObjective Objective) 
  where TGenotype : IGenotype where TObjective : IObjective;


public interface ICreator<TGenotype> where TGenotype : IGenotype { 
  TGenotype Create();
}

public interface ICrossover<TChromosome> where TChromosome : IChromosome { 
  TChromosome Crossover(TChromosome parent1, TChromosome parent2);
}


public class GenotypeCrossover<TChromosome> : ICrossover<Genotype<TChromosome>> where TChromosome : IChromosome {
  public GenotypeCrossover(ICrossover<TChromosome> chromosomeCrossover) {
    ChromosomeCrossover = chromosomeCrossover;
  }
  public ICrossover<TChromosome> ChromosomeCrossover { get; }
  public Genotype<TChromosome> Crossover(Genotype<TChromosome> parent1, Genotype<TChromosome> parent2) {
    return new Genotype<TChromosome>(ChromosomeCrossover.Crossover(parent1.Chromosome, parent2.Chromosome));
  }
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

public class OrderCrossover : ICrossover<Permutation> {
  public Permutation Crossover(Permutation parent1, Permutation parent2) { throw new NotImplementedException(); }
}



