using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
  int GetOffspringCount(int populationSize);
}

public abstract class Replacer<TGenotype, TEncoding, TProblem> : IReplacer<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
  
  public abstract int GetOffspringCount(int populationSize);
}

public abstract class Replacer<TGenotype, TEncoding> : IReplacer<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding);
  
  public abstract int GetOffspringCount(int populationSize);
  
  IReadOnlyList<Solution<TGenotype>> IReplacer<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Replace(previousPopulation, offspringPopulation, objective, random, encoding);
  }
}

public abstract class Replacer<TGenotype> : IReplacer<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);
  
  public abstract int GetOffspringCount(int populationSize);
  
  IReadOnlyList<Solution<TGenotype>> IReplacer<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>?> problem) {
    return Replace(previousPopulation, offspringPopulation, objective, random);
  }
}


public class PlusSelectionReplacer<TGenotype> : Replacer<TGenotype>
{
  public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    return combinedPopulation
      .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
      .Take(previousPopulation.Count) // if algorithm population differs from previousPopulation.Length, it is not detected
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }
}


public class ElitismReplacer<TGenotype> : Replacer<TGenotype>
{
  public int Elites { get; }
  
  public ElitismReplacer(int elites) {
    if (elites < 0) throw new ArgumentOutOfRangeException(nameof(elites), "Elites must be non-negative.");
    Elites = elites;
  }

  public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    if (Elites < 0) throw new ArgumentOutOfRangeException(nameof(Elites), "Elites must be non-negative.");
    
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
      .Take(Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) => populationSize - Elites;
}
