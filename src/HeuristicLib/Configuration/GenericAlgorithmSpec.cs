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
