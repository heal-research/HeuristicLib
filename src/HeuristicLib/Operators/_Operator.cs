using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator { }
//
// public interface IOperatorFactory { }
//
// public interface IOperatorFactory<out TOperator> : IOperatorFactory where TOperator : IOperator {
//   TOperator Create();
// }
//
// public class OperatorFactory<TOperator> : IOperatorFactory<TOperator> where TOperator : IOperator {
//   private readonly Func<TOperator> factory;
//   public OperatorFactory(Func<TOperator> factory) { this.factory = factory; }
//   public TOperator Create() => factory();
// }
//
// public static class OperatorFactory {
//   public static IOperatorFactory<TOperator> Create<TOperator>(Func<TOperator> factory) where TOperator : IOperator {
//     return new OperatorFactory<TOperator>(factory);
//   }
//   public static IOperatorFactory<TOperator> Create<TOperator>(TOperator @operator) where TOperator : IOperator {
//     return Create(() => @operator);
//   }
// }


// public interface IStochasticOperatorFactory : IOperatorFactory {
//   void SetRandom(IRandomSource randomSource);
// }
//
// public interface IEncodingDependentOperatorFactory<in TEncoding> : IOperatorFactory where TEncoding : IEncodingParameter {
//   void SetEncoding(TEncoding encoding);
// }
//
// public interface IProblemDependentOperatorFactory<in TProblem> : IOperatorFactory where TProblem : IProblem {
//   void SetProblem(TProblem problem);
// }

