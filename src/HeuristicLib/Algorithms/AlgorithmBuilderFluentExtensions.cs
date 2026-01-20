// using HEAL.HeuristicLib.Operators.Evaluators;
// using HEAL.HeuristicLib.Operators.Interceptors;
// using HEAL.HeuristicLib.Operators.Terminators;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.SearchSpaces;
// using HEAL.HeuristicLib.States;
//
// namespace HEAL.HeuristicLib.Algorithms;
//
// public static class AlgorithmBuilderFluentExtensions {
//   public static TBuilder WithEvaluator<TBuilder, TSpec, TG, TS, TP, TR, TA>(
//     this TBuilder builder,
//     IEvaluator<TG, TS, TP> evaluator)
//     where TSpec : class
//     where TG : class
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : class, IAlgorithmState
//     where TA : IAlgorithm<TG, TS, TP, TR>
//     where TBuilder : AlgorithmBuilder<TSpec, TG, TS, TP, TR, TA>
//   {
//     builder.Evaluator = evaluator;
//     return builder;
//   }
//
//   public static TBuilder WithTerminator<TBuilder, TSpec, TG, TS, TP, TR, TA>(
//     this TBuilder builder,
//     ITerminator<TG, TR, TS, TP> terminator)
//     where TSpec : class
//     where TG : class
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : class, IAlgorithmState
//     where TA : IAlgorithm<TG, TS, TP, TR>
//     where TBuilder : AlgorithmBuilder<TSpec, TG, TS, TP, TR, TA>
//   {
//     builder.Terminator = terminator;
//     return builder;
//   }
//
//   public static TBuilder WithInterceptor<TBuilder, TSpec, TG, TS, TP, TR, TA>(
//     this TBuilder builder,
//     IInterceptor<TG, TR, TS, TP>? interceptor)
//     where TSpec : class
//     where TG : class
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : class, IAlgorithmState
//     where TA : IAlgorithm<TG, TS, TP, TR>
//     where TBuilder : AlgorithmBuilder<TSpec, TG, TS, TP, TR, TA>
//   {
//     builder.Interceptor = interceptor;
//     return builder;
//   }
//
//   public static TBuilder WithFeature<TBuilder, TSpec, TG, TS, TP, TR, TA>(
//     this TBuilder builder,
//     IBuildFeature<TSpec> feature)
//     where TSpec : class
//     where TG : class
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : class, IAlgorithmState
//     where TA : IAlgorithm<TG, TS, TP, TR>
//     where TBuilder : AlgorithmBuilder<TSpec, TG, TS, TP, TR, TA>
//   {
//     builder.AddFeature(feature);
//     return builder;
//   }
// }
