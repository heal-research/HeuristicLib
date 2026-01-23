// namespace HEAL.HeuristicLib.Algorithms;
//
// public static class AlgorithmBuildExtensions {
//   public static TA Build<TSpec, TG, TS, TP, TR, TA>(this IAlgorithmBuilder<TSpec, TG, TS, TP, TR, TA> builder)
//     where TSpec : class
//     where TG : class
//     where TS : class, SearchSpaces.ISearchSpace<TG>
//     where TP : class, Problems.IProblem<TG, TS>
//     where TR : class, States.IAlgorithmState
//     where TA : IAlgorithm<TG, TS, TP, TR>
//   {
//     var spec = builder.CreateSpec();
//
//     foreach (var feature in builder.Features) {
//       spec = feature.Apply(spec);
//     }
//
//     return builder.CreateAlgorithm(spec);
//   }
// }


