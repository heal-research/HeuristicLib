using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmConfiguration<TGenotype, TEncodingParameter>(
  int? PopulationSize = null,
  //int? MaximumGenerations = null,
  ICreator<TGenotype, TEncodingParameter>? Creator = null,
  ICrossover<TGenotype, TEncodingParameter>? Crossover = null,
  IMutator<TGenotype, TEncodingParameter>? Mutator = null,
  double? MutationRate = null,
  ISelector? Selector = null,
  IReplacer? Replacer = null
  //int? RandomSeed = null
) where TEncodingParameter : IEncodingParameter<TGenotype>;

public static class GeneticAlgorithmBuilderWithSpecsExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithConfiguration<TGenotype, TPhenotype, TEncodingParameter>(
    this GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> builder, GeneticAlgorithmConfiguration<TGenotype, TEncodingParameter> gaRef)
    where TEncodingParameter : IEncodingParameter<TGenotype> 
  {
    if (gaRef.PopulationSize.HasValue) builder.WithPopulationSize(gaRef.PopulationSize.Value);
    if (gaRef.Creator is not null) builder.WithCreator(gaRef.Creator);
    if (gaRef.Crossover is not null) builder.WithCrossover(gaRef.Crossover);
    if (gaRef.Mutator is not null) builder.WithMutator(gaRef.Mutator);
    if (gaRef.MutationRate.HasValue) builder.WithMutationRate(gaRef.MutationRate.Value);
    if (gaRef.Selector is not null) builder.WithSelector(gaRef.Selector);
    if (gaRef.Replacer is not null) builder.WithReplacer(gaRef.Replacer);
    // if (gaRef.MaximumGenerations.HasValue) builder.WithTerminator(Terminator.OnGeneration(gaRef.MaximumGenerations.Value));
    // if (gaRef.RandomSeed.HasValue) builder.WithRandomSource(new RandomSource(gaRef.RandomSeed.Value));

    return builder;
  }
}
