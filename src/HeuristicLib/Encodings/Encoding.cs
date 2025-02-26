using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding {}

public interface IEncoding<TGenotype> : IEncoding {
  bool IsValidGenotype(TGenotype genotype);
  bool AreCompatible(params IEnumerable<TGenotype> genotypes);
}

public abstract class EncodingBase<TGenotype> : IEncoding<TGenotype> {
  
  protected EncodingBase() { }
  

  public abstract bool IsValidGenotype(TGenotype genotype);
  public virtual bool AreCompatible(params IEnumerable<TGenotype> genotypes) {
    return genotypes.All(IsValidGenotype);
  }
}


public interface IEncodingBasedOperator<out TOperator, in TEncoding> {
  static abstract TOperator FromEncoding(TEncoding encoding);
}



public interface IConfigurationBundle<TGenotype> {
  
}

public interface IEncodingBundle<TGenotype, TEncoding> : IConfigurationBundle<TGenotype> {
  TEncoding Encoding { get; }
}

public interface ICreatorProvider<TGenotype>
{
  ICreator<TGenotype> Creator { get; }
}

public interface ICrossoverProvider<TGenotype>
{
  ICrossover<TGenotype> Crossover { get; }
}

public interface IMutatorProvider<TGenotype>
{
  IMutator<TGenotype> Mutator { get; }
}

public static class GeneticAlgorithmBuilderEncodingExtension {
  public static GeneticAlgorithmBuilder<TSolution> WithEncodingBundle<TEncoding, TSolution>(this GeneticAlgorithmBuilder<TSolution> builder, IEncodingBundle<TSolution, TEncoding> encoding)
  {
    if (encoding is ICreatorProvider<TSolution> creatorProvider) {
      builder.WithCreator(creatorProvider.Creator);
    }
    if (encoding is ICrossoverProvider<TSolution> crossoverProvider) {
      builder.WithCrossover(crossoverProvider.Crossover);
    }
    if (encoding is IMutatorProvider<TSolution> mutatorProvider) {
      builder.WithMutation(mutatorProvider.Mutator);
    }
    return builder;
  }
}
