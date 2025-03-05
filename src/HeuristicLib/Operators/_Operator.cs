using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator {
}

public abstract class Operator : IOperator {
  protected Operator() { }
}

public interface IEncodingOperator<TGenotype, out TEncoding> : IOperator where TEncoding : IEncoding<TGenotype, TEncoding> {
  TEncoding Encoding { get; }
}

public abstract class EncodingOperator<TGenotype, TEncoding> : Operator, IEncodingOperator<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
  public TEncoding Encoding { get; }
  protected EncodingOperator(TEncoding encoding) {
    Encoding = encoding;
  }
}
