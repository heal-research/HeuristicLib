using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ISelector<TGenotype, in TEncoding> : ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ISelector<TGenotype> : ISelector<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public abstract class BatchSelector<TGenotype, TEncoding> : ISelector<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Select(population, objective, count, random, encoding);
  }
}

public abstract class BatchSelector<TGenotype> : ISelector<TGenotype> {
  public abstract IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Select(population, objective, count, random);
  }

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Select(population, objective, count, random);
  }
}

public abstract class Selector<TGenotype, TEncoding, TProblem> : ISelector<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract Solution<TGenotype> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var selected = new Solution<TGenotype>[count];
    Parallel.For(0, selected.Length, i => {
      selected[i] = Select(population, objective, random, encoding, problem);
    });
    return selected;
  }
}

public abstract class Selector<TGenotype, TEncoding> : ISelector<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract Solution<TGenotype> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding) {
    var selected = new Solution<TGenotype>[count];
    Parallel.For(0, selected.Length, i => {
      selected[i] = Select(population, objective, random, encoding);
    });
    return selected;
  }

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Select(population, objective, count, random, encoding);
  }
}

public abstract class Selector<TGenotype> : ISelector<TGenotype> {
  public abstract Solution<TGenotype> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, IRandomNumberGenerator random);

  public IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype>[count];
    Parallel.For(0, selected.Length, i => {
      selected[i] = Select(population, objective, random);
    });
    return selected;
  }

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Select(population, objective, count, random);
  }

  IReadOnlyList<Solution<TGenotype>> ISelector<TGenotype, IEncoding<TGenotype>>.Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Select(population, objective, count, random);
  }
}
