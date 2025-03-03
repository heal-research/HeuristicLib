namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype> : IOperator
{
  TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public interface ICrossoverTemplate<out TCrossover, TGenotype, in TParams> 
  : IOperatorTemplate<TCrossover, TParams>
  where TCrossover : ICrossover<TGenotype> 
  where TParams : CrossoverParameters {
}

public record CrossoverParameters();

public abstract class CrossoverBase<TGenotype> : ICrossover<TGenotype> {
  public abstract TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public abstract class CrossoverTemplateBase<TCrossover, TGenotype, TParams> 
  : ICrossoverTemplate<TCrossover, TGenotype, TParams> 
  where TCrossover : ICrossover<TGenotype> 
  where TParams : CrossoverParameters {
  public abstract TCrossover Parameterize(TParams parameters);
}




public interface IRecordGenotypeBase<TSelf, T1, T2> where TSelf : IRecordGenotypeBase<TSelf, T1, T2> {
  static abstract TSelf Construct(T1 item1, T2 item2);
  void Deconstruct(out T1 item1, out T2 item2);
}

public class RecordCrossover<T, T1, T2> : CrossoverBase<T> where T : IRecordGenotypeBase<T, T1, T2> {
  private readonly ICrossover<T1> crossover1;
  private readonly ICrossover<T2> crossover2;

  public RecordCrossover(ICrossover<T1> crossover1, ICrossover<T2> crossover2) {
    this.crossover1 = crossover1;
    this.crossover2 = crossover2;
  }

  public override T Crossover(T parent1, T parent2) {
    var (parent1Chromosome1, parent1Chromosome2) = parent1;
    var (parent2Chromosome1, parent2Chromosome2) = parent2;
    var child1 = crossover1.Crossover(parent1Chromosome1, parent2Chromosome1);
    var child2 = crossover2.Crossover(parent1Chromosome2, parent2Chromosome2);
    return T.Construct(child1, child2);
  }
}
