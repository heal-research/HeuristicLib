using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossover;

public interface IParents<out T> {
  T Parent1 { get; }
  T Parent2 { get; }

  T Item1 => Parent1;
  T Item2 => Parent2;
}

public record Parents<T>(T Parent1, T Parent2) : IParents<T>;

public interface ICrossover<TGenotype> : ICrossover<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random);
}

public interface ICrossover<TGenotype, in TEncoding> : ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}
