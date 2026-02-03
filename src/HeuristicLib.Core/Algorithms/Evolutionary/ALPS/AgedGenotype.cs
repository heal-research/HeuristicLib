namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public readonly record struct AgedGenotype<TGenotype>(TGenotype InnerGenotype, int Age);
