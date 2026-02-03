using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossover;

public abstract class Crossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r, encoding, problem));
  }
}

public abstract class Crossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TEncoding encoding) {
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r, encoding));
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Cross(parents, random, encoding);
  }
}

public abstract class Crossover<TGenotype> : ICrossover<TGenotype> {
  public abstract TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) {
    return parents.ParallelSelect(random, (_, x, r) => Cross(x, r));
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, IEncoding<TGenotype> searchSpace, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Cross(parents, random);
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>>.Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, IEncoding<TGenotype> searchSpace) {
    return Cross(parents, random);
  }
}

public class NoCrossOver<TGenotype> : BatchCrossover<TGenotype> {
  public static readonly NoCrossOver<TGenotype> Instance = new();

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) {
    return parents.Select(x => x.Parent1).ToArray();
  }
}
