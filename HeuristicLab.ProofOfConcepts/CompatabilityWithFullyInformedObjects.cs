
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingFullyInformedObjects;



public interface IOperator
{
  string Name { get; }
  Type? GenotypeType { get; }
  Type? EncodingType { get; }
  Type? ProblemType { get; }
  Type? AlgorithmType { get; }
}

public interface ICrossover : IOperator
{
  TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
}

public abstract class Crossover : ICrossover
{
  public string Name { get; }
  public Type? GenotypeType => null;
  public Type? EncodingType => null;
  public Type? ProblemType => null;
  public Type? AlgorithmType => null;

  protected Crossover(string name) {
    Name = name;
  }
  
  public abstract TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
}

public abstract class Crossover<TGenotype> : ICrossover
{
  public string Name { get; }
  public Type? GenotypeType { get; }
  public Type? EncodingType => null;
  public Type? ProblemType => null;
  public Type? AlgorithmType => null;

  protected Crossover(string name) {
    Name = name;
    GenotypeType = typeof(TGenotype);
  }
  
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2);

  public TGenotype2 Cross<TGenotype2>(TGenotype2 parent1, TGenotype2 parent2) {
    if
  }
}
