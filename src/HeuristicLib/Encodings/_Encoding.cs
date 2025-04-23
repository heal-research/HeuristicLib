using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding { }

public interface IEncoding<in TGenotype> : IEncoding {
  bool IsValidGenotype(TGenotype genotype);
  //bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator);
}

public abstract record class EncodingBase<TGenotype> : IEncoding<TGenotype> {
  protected EncodingBase() { }
  public abstract bool IsValidGenotype(TGenotype genotype);
  // public virtual bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator) {
  //   return @operator.Encoding.Equals(this);
  // }
}
