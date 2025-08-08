using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}


public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding);
  
  TGenotype ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(random, encoding);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract TGenotype Create(IRandomNumberGenerator random);
  
  TGenotype ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Create(random);
  }
}



public class PredefinedSolutionsCreator<TGenotype, TEncoding, TProblem> : Creator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  private readonly TGenotype[] predefinedSolutions;
  private readonly ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions;

  private int currentSolutionIndex = 0;
  
  public PredefinedSolutionsCreator(TGenotype[] predefinedSolutions, ICreator<TGenotype, TEncoding, TProblem> creatorForRemainingSolutions) {
    this.predefinedSolutions = predefinedSolutions;
    this.creatorForRemainingSolutions = creatorForRemainingSolutions;
  }

  public override TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    if (currentSolutionIndex < predefinedSolutions.Length) {
      return predefinedSolutions[currentSolutionIndex++];
    } else {
      return creatorForRemainingSolutions.Create(random, encoding, problem);
    }
  }
}
