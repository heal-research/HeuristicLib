using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public record GeneticAlgorithmSpec(
  int? PopulationSize = null,
  int? MaximumGenerations = null,
  CreatorSpec? Creator = null,
  CrossoverSpec? Crossover = null,
  MutatorSpec? Mutator = null,
  double? MutationRate = null,
  SelectorSpec? Selector = null,
  ReplacerSpec? Replacer = null,
  int? RandomSeed = null
);

public static class GeneticAlgorithmBuilderWithSpecsExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithSpecs<TGenotype, TEncodingParameter>(this GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> builder, GeneticAlgorithmSpec gaSpec)
    where TEncodingParameter : IEncodingParameter<TGenotype> 
  {
    if (gaSpec.PopulationSize.HasValue) builder.WithPopulationSize(gaSpec.PopulationSize.Value);
    if (gaSpec.Creator is not null) builder.WithCreator(enc => gaSpec.Creator.CreateCreator<TGenotype, TEncodingParameter>(enc));
    if (gaSpec.Crossover is not null) builder.WithCrossover(enc => gaSpec.Crossover.CreateCrossover<TGenotype, TEncodingParameter>(enc));
    if (gaSpec.Mutator is not null) builder.WithMutator(enc => gaSpec.Mutator.CreateMutator<TGenotype, TEncodingParameter>(enc));
    if (gaSpec.MutationRate.HasValue) builder.WithMutationRate(gaSpec.MutationRate.Value);
    if (gaSpec.Selector is not null) builder.WithSelector(gaSpec.Selector.CreateSelector());
    if (gaSpec.Replacer is not null) builder.WithReplacer(gaSpec.Replacer.CreateReplacer());
    if (gaSpec.MaximumGenerations.HasValue) builder.WithTerminator(Terminator.OnGeneration(gaSpec.MaximumGenerations.Value));
    if (gaSpec.RandomSeed.HasValue) builder.WithRandomSource(new RandomSource(gaSpec.RandomSeed.Value));

    return builder;
  }
}
