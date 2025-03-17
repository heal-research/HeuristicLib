using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator { }


// public abstract class OperatorBase<TGenotype> : IOperator<TGenotype> {
//   protected OperatorBase() { }
// }


public interface IContext {}

public interface IEncodingContext<out TEncoding> : IContext where TEncoding : IEncoding {
  TEncoding Encoding { get; }
}

public interface IRandomContext : IContext {
  IRandomNumberGenerator Random { get; }
}

// public interface IEncodingOperator<TGenotype, out TEncoding> : IOperator where TEncoding : IEncoding<TGenotype, TEncoding> {
//   TEncoding Encoding { get; }
// }
//
// public abstract class EncodingOperator<TGenotype, TEncoding> : Operator, IEncodingOperator<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
//   public TEncoding Encoding { get; }
//   protected EncodingOperator(TEncoding encoding) {
//     Encoding = encoding;
//   }
// }
