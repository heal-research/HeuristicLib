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
