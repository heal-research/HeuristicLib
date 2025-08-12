using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding, TProblem> context);
}

public interface ICreator<TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding> context);
}

public interface ICreator<TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>>
{
  IReadOnlyList<TGenotype> Create(int count, IExecutionContext context);
}


public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding, TProblem> context);
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding> context);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IExecutionContext<TEncoding, IProblem<TGenotype, TEncoding>> context) {
    return Create(count, context);
  }
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Create(int count, IExecutionContext context);
  
  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IExecutionContext<IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> context) {
    return Create(count, context);
  }
  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IExecutionContext<IEncoding<TGenotype>> context) {
    return Create(count, context);
  }
}


public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Create(IExecutionContext<TEncoding, TProblem> context);

  public IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding, TProblem> context) {
    return context.ParallelFor(0, count, Create);
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Create(IExecutionContext<TEncoding> context);
  
  public IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding> context) {
    return context.ParallelFor(0, count, Create);
  }
  
  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IExecutionContext<TEncoding, IProblem<TGenotype, TEncoding>> context) {
    return Create(count, context);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype>
{
  public abstract TGenotype Create(IExecutionContext context);
  
  public IReadOnlyList<TGenotype> Create(int count, IExecutionContext context) {
    return context.ParallelFor(0, count, Create);
  }
  
  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IExecutionContext<IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> context) {
    return Create(count, context);
  }
  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IExecutionContext<IEncoding<TGenotype>> context) {
    return Create(count, context);
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

  public override IReadOnlyList<TGenotype> Create(int count, IExecutionContext<TEncoding, TProblem> context) {
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
      var remaining = creatorForRemainingSolutions.Create(countRemaining, context);
      for (int i = 0; i < countRemaining; i++) {
        offspring[countPredefined + i] = remaining[i];
      }
    }

    return offspring;
  }
}
