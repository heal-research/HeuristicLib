using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract class Crossover<TGenotype, TSearchSpace, TProblem> : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => parents.ParallelSelect(random, action: (_, x, r) => Cross(x, r, encoding, problem));
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

public abstract class Crossover<TGenotype, TSearchSpace> : ICrossover<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace encoding) => parents.ParallelSelect(random, action: (_, x, r) => Cross(x, r, encoding));

  IReadOnlyList<TGenotype> ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Cross(parents, random, encoding);
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TSearchSpace encoding);
}

public abstract class Crossover<TGenotype> : ICrossover<TGenotype>
{

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) => parents.ParallelSelect(random, action: (_, x, r) => Cross(x, r));

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Cross(parents, random);

  IReadOnlyList<TGenotype> ICrossover<TGenotype, ISearchSpace<TGenotype>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace) => Cross(parents, random);
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);
}

public class NoCrossOver<TGenotype> : BatchCrossover<TGenotype>
{
  public static readonly NoCrossOver<TGenotype> Instance = new();

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) => parents.Select(x => x.Parent1).ToArray();
}
