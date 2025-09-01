using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class MemoryList<T> : IReadOnlyList<T> {
  private readonly ReadOnlyMemory<T> memory;

  public MemoryList(ReadOnlyMemory<T> memory) => this.memory = memory;

  public int Count => memory.Length;

  public T this[int index] => memory.Span[index];

  public IEnumerator<T> GetEnumerator() {
    for (int i = 0; i < memory.Length; i++)
      yield return memory.Span[i];
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface ICreator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, TProblem problem, Memory<TGenotype>? buffer = null);
}

public interface ICreator<TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, Memory<TGenotype>? buffer = null);
}

public interface ICreator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Create(int count, IRandom random, Memory<TGenotype>? buffer = null);
}

public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, TProblem problem, Memory<TGenotype>? buffer = null);
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, Memory<TGenotype>? buffer = null);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandom random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem, Memory<TGenotype>? buffer) {
    return Create(count, random, encoding, buffer);
  }
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandom random, Memory<TGenotype>? buffer = null);

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandom random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem, Memory<TGenotype>? buffer) {
    return Create(count, random, buffer);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IRandom random, IEncoding<TGenotype> encoding, Memory<TGenotype>? buffer) {
    return Create(count, random, buffer);
  }
}

public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TGenotype Create(IRandom random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, TProblem problem, Memory<TGenotype>? buffer = null) {
    if (buffer.HasValue && buffer.Value.Length < count) throw new ArgumentException("Offspring buffer is smaller than count.");
    var offspring = buffer ?? new TGenotype[count];

    if (offspring.Length < count) throw new ArgumentException("Offspring span is smaller than count.");
    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring.Span[i] = Create(randoms[i], encoding, problem));

    return new MemoryList<TGenotype>(offspring);
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TGenotype Create(IRandom random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, Memory<TGenotype>? buffer = null) {
    if (buffer.HasValue && buffer.Value.Length < count) throw new ArgumentException("Offspring buffer is smaller than count.");
    var offspring = buffer ?? new TGenotype[count];

    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring.Span[i] = Create(randoms[i], encoding));

    return new MemoryList<TGenotype>(offspring);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandom random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem, Memory<TGenotype>? buffer) {
    return Create(count, random, encoding, buffer);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create(IRandom random);

  public IReadOnlyList<TGenotype> Create(int count, IRandom random, Memory<TGenotype>? buffer = null) {
    if (buffer.HasValue && buffer.Value.Length < count) throw new ArgumentException("Offspring buffer is smaller than count.");
    var offspring = buffer ?? new TGenotype[count];

    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring.Span[i] = Create(randoms[i]));

    return new MemoryList<TGenotype>(offspring);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandom random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem, Memory<TGenotype>? buffer) {
    return Create(count, random, buffer);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IRandom random, IEncoding<TGenotype> encoding, Memory<TGenotype>? buffer) {
    return Create(count, random, buffer);
  }
}

public class PredefinedSolutionsCreator<TGenotype, TEncoding, TProblem> : BatchCreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly TGenotype[] predefinedSolutions;
  private readonly ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions;

  private int currentSolutionIndex = 0;

  public PredefinedSolutionsCreator(IReadOnlyList<TGenotype> predefinedSolutions, ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions) {
    this.predefinedSolutions = predefinedSolutions.ToArray();
    this.creatorForRemainingSolutions = creatorForRemainingSolutions;
  }

  public override IReadOnlyList<TGenotype> Create(int count, IRandom random, TEncoding encoding, TProblem problem, Memory<TGenotype>? buffer = null) {
    if (buffer.HasValue && buffer.Value.Length < count) throw new ArgumentException("Offspring buffer is smaller than count.");
    var offspring = buffer ?? new TGenotype[count];

    int countPredefined = Math.Min(predefinedSolutions.Length - currentSolutionIndex, count);
    if (countPredefined > 0) {
      var predefinedSpan = predefinedSolutions.AsSpan(currentSolutionIndex, countPredefined);
      predefinedSpan.CopyTo(offspring.Span);
      currentSolutionIndex += countPredefined;
    }

    int countRemaining = count - countPredefined;
    if (countRemaining > 0) {
      var remainingRandom = random.Fork("remaining");
      var remainingSpan = offspring.Slice(countPredefined, countRemaining);
      creatorForRemainingSolutions.Create(countRemaining, remainingRandom, encoding, problem, remainingSpan);
    }

    return new MemoryList<TGenotype>(offspring);
  }
}
