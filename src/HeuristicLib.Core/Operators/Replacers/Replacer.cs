using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract class Replacer<TGenotype, TSearchSpace, TProblem> : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);

  public abstract int GetOffspringCount(int populationSize);
}

public abstract class Replacer<TGenotype, TSearchSpace> : IReplacer<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacer<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Replace(previousPopulation, offspringPopulation, objective, random, encoding);
}

public abstract class Replacer<TGenotype> : IReplacer<TGenotype>
{
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacer<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Replace(previousPopulation, offspringPopulation, objective, random);

  IReadOnlyList<ISolution<TGenotype>> IReplacer<TGenotype, ISearchSpace<TGenotype>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Replace(previousPopulation, offspringPopulation, objective, random);
}
