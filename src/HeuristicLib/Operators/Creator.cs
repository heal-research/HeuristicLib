using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Creator<TGenotype, TSearchSpace, TProblem> : Operator<TGenotype, TSearchSpace, TProblem, ICreatorExecution<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Creator<TGenotype, TSearchSpace, TProblem>?(Creator<TGenotype, TSearchSpace>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificCreator<TGenotype, TSearchSpace, TProblem>(problemAgnosticOperator);
  }
}

public abstract record class Creator<TGenotype, TSearchSpace> : Operator<TGenotype, TSearchSpace, ICreatorExecution<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}

public interface ICreatorExecution<TGenotype>
{
  TGenotype Create(IRandomNumberGenerator random);
}

public abstract class CreatorExecution<TGenotype, TSearchSpace, TProblem, TCreator> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TCreator>, ICreatorExecution<TGenotype> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  protected CreatorExecution(TCreator parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) { }
  public abstract TGenotype Create(IRandomNumberGenerator random);
}

public abstract class CreatorExecution<TGenotype, TSearchSpace, TCreator> : OperatorExecution<TGenotype, TSearchSpace, TCreator>, ICreatorExecution<TGenotype> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected CreatorExecution(TCreator parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  
  public abstract TGenotype Create(IRandomNumberGenerator random);
}

// public static class Creator {
//   public static CustomCreator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TSearchSpace, IRandomNumberGenerator, TGenotype> creator) 
//     where TSearchSpace : ISearchSpace<TGenotype> 
//   {
//     return new CustomCreator<TGenotype, TSearchSpace>(creator);
//   }
// }
//
// public sealed class CustomCreator<TGenotype, TSearchSpace> 
//   : ICreator<TGenotype, TSearchSpace> 
//   where TSearchSpace : ISearchSpace<TGenotype> {
//   private readonly Func<TSearchSpace, IRandomNumberGenerator, TGenotype> creator;
//   internal CustomCreator(Func<TSearchSpace, IRandomNumberGenerator, TGenotype> creator) {
//     this.creator = creator;
//   }
//   public TGenotype Create(TSearchSpace searchSpace, IRandomNumberGenerator random) => creator(searchSpace, random);
// }
//
// public abstract class CreatorBase<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace> where TSearchSpace : ISearchSpace<TGenotype> {
//   public abstract TGenotype Create(TSearchSpace searchSpace, IRandomNumberGenerator random);
// }


public sealed record class ProblemSpecificCreator<TGenotype, TSearchSpace, TProblem> : Creator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public Creator<TGenotype, TSearchSpace> ProblemAgnosticCreator { get; }

  public ProblemSpecificCreator(Creator<TGenotype, TSearchSpace> problemAgnosticCreator) {
    ProblemAgnosticCreator = problemAgnosticCreator;
  }
  
  public override ICreatorExecution<TGenotype> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return ProblemAgnosticCreator.CreateExecution(searchSpace);
  }
}
