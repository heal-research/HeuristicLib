using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding {}

public interface IEncoding<TGenotype> : IEncoding {
  // ICreator<TSolution> Creator { get; }
  // IMutator<TSolution> Mutator { get; }
  // ICrossover<TSolution> Crossover { get; }
  
  bool IsValid(TGenotype genotype);
  bool AreCompatible(params IEnumerable<TGenotype> genotypes);
}

public abstract class EncodingBase<TGenotype> : IEncoding<TGenotype> {
  public abstract bool IsValid(TGenotype genotype);
  public virtual bool AreCompatible(params IEnumerable<TGenotype> genotypes) {
    return genotypes.All(IsValid);
  }
}

public interface IEncodingSpecificOperator<out TEncoding, TGenotype> 
  where TEncoding : IEncoding<TGenotype>
{
  TEncoding Encoding { get; }
}

public abstract class EncodingSpecificOperator<TEncoding, TGenotype>
  : IEncodingSpecificOperator<TEncoding, TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  protected EncodingSpecificOperator(TEncoding encoding) {
    Encoding = encoding;
  }

  public TEncoding Encoding { get; }
}
