using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selector;

public class EliteSelector<TGenotype, TEncoding, TProblem> : BatchSelector<TGenotype, TEncoding, TProblem> where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly BestSelector<TGenotype> best = new();

  public EliteSelector(ISelector<TGenotype, TEncoding, TProblem> selector, int elites = 1) {
    Selector = selector;
    Elites = elites;
  }

  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public int Elites { get; }

  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return best.Select(population, objective, Elites, random).Concat(Selector.Select(population, objective, count - Elites, random, encoding, problem)).ToArray();
  }
}

public static class EliteSelector {
  public static EliteSelector<TGenotype, TEncoding, TProblem> WithElites<TGenotype, TEncoding, TProblem>(this ISelector<TGenotype, TEncoding, TProblem> selector, int elites = 1)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new EliteSelector<TGenotype, TEncoding, TProblem>(selector, elites);
  }
}
