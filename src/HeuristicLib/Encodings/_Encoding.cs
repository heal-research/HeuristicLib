using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding { }

public interface IEncoding<TGenotype, in TSelf> : IEncoding where TSelf : IEncoding<TGenotype, TSelf> {
  bool IsValidGenotype(TGenotype genotype);
  bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator);
}

public abstract record class EncodingBase<TGenotype, TSelf> : IEncoding<TGenotype, TSelf> where TSelf : EncodingBase<TGenotype, TSelf> {
  protected EncodingBase() { }
  public abstract bool IsValidGenotype(TGenotype genotype);
  public virtual bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator) {
    return @operator.Encoding.Equals(this);
  }
}
