using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public interface ICreator<TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
{
  void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICreator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>>
{
  void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random);
}

public static class CreatorExtensions
{
  public static IReadOnlyList<TGenotype> Create<TGenotype, TEncoding, TProblem>(this ICreator<TGenotype, TEncoding, TProblem> creator, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> 
  {
    var offspring = new TGenotype[count];
    creator.Create(count, offspring, random, encoding, problem);
    return offspring;
  }
  
  public static IReadOnlyList<TGenotype> Create<TGenotype, TEncoding>(this ICreator<TGenotype, TEncoding> creator, int count, IRandomNumberGenerator random, TEncoding encoding)
    where TEncoding : class, IEncoding<TGenotype>
  {
    var offspring = new TGenotype[count];
    creator.Create(count, offspring, random, encoding, null!);
    return offspring;
  }
  
  public static IReadOnlyList<TGenotype> Create<TGenotype>(this ICreator<TGenotype> creator, int count, IRandomNumberGenerator random)
  {
    var offspring = new TGenotype[count];
    creator.Create(count, offspring, random, null!, null!);
    return offspring;
  }
}


public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding);

  void ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    Create(count, offspring, random, encoding);
  }
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype>
{
  public abstract void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random);
  
  void ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    Create(count, offspring, random);
  }
  void ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, Memory<TGenotype> offspring,  IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    Create(count, offspring, random);
  }
}


public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    if (offspring.Length < count) throw new ArgumentException("Offspring span is smaller than count.");
    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring.Span[i] = Create(randoms[i], encoding, problem));
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding);
  
  public void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding) {
    if (offspring.Length < count) throw new ArgumentException("Offspring span is smaller than count.");
    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring.Span[i] = Create(randoms[i], encoding));
  }
  
  void ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    Create(count, offspring, random, encoding);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random);
  
  public void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random) {
    if (offspring.Length < count) throw new ArgumentException("Offspring span is smaller than count.");
    var randoms = random.Spawn(count);
    Parallel.For(0, count, i => offspring.Span[i] = Create(randoms[i]));
  }
  
  void ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    Create(count, offspring, random);
  }
  void ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    Create(count, offspring, random);
  }
}



public class PredefinedSolutionsCreator<TGenotype, TEncoding, TProblem> : BatchCreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  private readonly TGenotype[] predefinedSolutions;
  private readonly ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions;

  private int currentSolutionIndex = 0;
  
  public PredefinedSolutionsCreator(IReadOnlyList<TGenotype> predefinedSolutions, ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions) {
    this.predefinedSolutions = predefinedSolutions.ToArray();
    this.creatorForRemainingSolutions = creatorForRemainingSolutions;
  }

  public override void Create(int count, Memory<TGenotype> offspring, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    if (offspring.Length < count) throw new ArgumentException("Offspring span is smaller than count.");
    
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
      creatorForRemainingSolutions.Create(countRemaining, remainingSpan, remainingRandom, encoding, problem);
    }
  }
}
