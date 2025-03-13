// using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
// using HEAL.HeuristicLib.Encodings;
//
// namespace HEAL.HeuristicLib.Configuration;
//
// public interface IEncodingProvider<out TEncoding> {
//   TEncoding CreateEncoding();
// }
//
// public interface IAlgorithmConfigProvider<out TConfigSpec, in TEncoding> {
//   TConfigSpec CreateDefaultConfig(TEncoding encoding);
// }
//
//
// public static class BuilderExtensions {
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithEncodingProvider<TEncoding, TGenotype>
//     (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, IEncodingProvider<TEncoding> encodingProvider)
//     where TEncoding : IEncoding<TGenotype, TEncoding> {
//     var encoding = encodingProvider.CreateEncoding();
//     builder.WithEncoding(encoding);
//     return builder;
//   }
// }
