using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
   IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public interface ICreator<TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICreator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(count, random, encoding);
  }
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype>
{
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
  public abstract TGenotype Create(IRandom random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[count];
    if (offspring.Length < count) throw new ArgumentException("Offspring span is smaller than count.");
    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring[i] = Create(randoms[i], encoding, problem));

    return offspring;
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
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

public abstract class Creator<TGenotype> : ICreator<TGenotype>
{
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

public class PredefinedSolutionsCreator<TGenotype, TEncoding, TProblem> : BatchCreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  private readonly IReadOnlyList<TGenotype> predefinedSolutions;
  private readonly ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions;

  private int currentSolutionIndex = 0;

  public PredefinedSolutionsCreator(IReadOnlyList<TGenotype> predefinedSolutions, ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions) {
    this.predefinedSolutions = predefinedSolutions;
    this.creatorForRemainingSolutions = creatorForRemainingSolutions;
  }

  public override IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[count];
    
    int countPredefined = Math.Min(predefinedSolutions.Count - currentSolutionIndex, count);
    if (countPredefined > 0) {
      for (int i = 0; i < countPredefined; i++) {
        offspring[i] = predefinedSolutions[currentSolutionIndex + i];
      }
      currentSolutionIndex += countPredefined;
    }

    int countRemaining = count - countPredefined;
    if (countRemaining > 0) {
      var remainingRandom = random.Fork("remaining");
      var remaining = creatorForRemainingSolutions.Create(countRemaining, remainingRandom, encoding, problem);
      for (int i = 0; i < remaining.Count; i++) {
        offspring[countPredefined + i] = remaining[i];
      }
    }
    
    return offspring;
  }
}
