// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
//
// namespace HEAL.HeuristicLib.Algorithms;
//
// public interface IAlgorithmBuilder<out TAlgorithm>
//   where TAlgorithm : IAlgorithm
// {
//   TAlgorithm Create();
// }
//
//
// public interface IAlgorithmBuilder<TGenotype, TEncoding, out TAlgorithm>
//   where TEncoding : class, IEncoding<TGenotype>
//   where TAlgorithm : IAlgorithm<TGenotype, TEncoding>
// {
//   TAlgorithm Create();
// }
//
// public interface IAlgorithmBuilder<TGenotype, TEncoding, TProblem, out TAlgorithm>
//   where TEncoding : class, IEncoding<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TEncoding>
//   where TAlgorithm : IAlgorithm<TGenotype, TEncoding, TProblem>
// {
//   TAlgorithm Create();
// }
