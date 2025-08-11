using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}


public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(random, encoding);
  }
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding);
  
  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Create(count, random, encoding);
  }
}


public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem);


  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, TProblem>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var offspring = new TGenotype[count];
    Parallel.For(0, count, i => {
      offspring[i] = Create(random, encoding, problem);
    });
    return offspring;
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding);
  
  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    var offspring = new TGenotype[count];
    Parallel.For(0, count, i => {
      offspring[i] = Create(random, encoding);
    });
    return offspring;
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract TGenotype Create(IRandomNumberGenerator random);
  
  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    var offspring = new TGenotype[count];
    Parallel.For(0, count, i => {
      offspring[i] = Create(random);
    });
    return offspring;
  }
}



public class PredefinedSolutionsCreator<TGenotype, TEncoding, TProblem> : BatchCreator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
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
      var remaining = creatorForRemainingSolutions.Create(countRemaining, random, encoding, problem);
      for (int i = 0; i < countRemaining; i++) {
        offspring[countPredefined + i] = remaining[i];
      }
    }

    return offspring;
  }
}
