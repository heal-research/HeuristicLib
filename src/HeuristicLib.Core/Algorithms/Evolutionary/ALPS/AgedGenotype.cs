namespace HEAL.HeuristicLib.Algorithms.ALPS;

public readonly record struct AgedGenotype<TGenotype>(TGenotype InnerGenotype, int Age);
