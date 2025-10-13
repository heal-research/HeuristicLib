using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICreator<out TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(count, random, encoding);
  }
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Create(count, random);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Create(count, random);
  }
}

public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[count];
    if (offspring.Length < count)
      throw new ArgumentException("Offspring span is smaller than count.");
    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring[i] = Create(randoms[i], encoding, problem));

    return offspring;
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding) {
    var offspring = new TGenotype[count];

    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring[i] = Create(randoms[i], encoding));

    return offspring;
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(count, random, encoding);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) {
    var offspring = new TGenotype[count];

    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring[i] = Create(randoms[i]));

    return offspring;
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Create(count, random);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Create(count, random);
  }
}
