using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator {
}

public interface IOperatorTemplate<out TOperator, in TParameters>
  where TOperator : IOperator {
  TOperator Parameterize(TParameters parameters);
}



public interface IEncodingOperator<TGenotype, TEncoding> : IOperator where TEncoding : IEncoding<TGenotype> {
  TEncoding Encoding { get; }
}

public static class EncodingOperatorExtensions {
  public static bool IsValidOperator<TEncoding, TGenotype>(this TEncoding encoding, IEncodingOperator<TGenotype, TEncoding> @operator) where TEncoding : IEncoding<TGenotype> {
    return @operator.Encoding.Equals(encoding);
  }
  public static bool AreCompatible<TEncoding, TGenotype>(this TEncoding encoding, params IEnumerable<IEncodingOperator<TGenotype, TEncoding>> operators) where TEncoding : IEncoding<TGenotype> {
    return operators.All(op => encoding.IsValidOperator(op));
  }
}
