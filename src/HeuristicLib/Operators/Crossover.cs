using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Crossover<TGenotype, TSearchSpace, TProblem> : Operator<TGenotype, TSearchSpace, TProblem, ICrossoverExecution<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Crossover<TGenotype, TSearchSpace, TProblem>?(Crossover<TGenotype, TSearchSpace>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificCrossover<TGenotype, TSearchSpace, TProblem>(problemAgnosticOperator);
  }
}

public abstract record class Crossover<TGenotype, TSearchSpace> : Operator<TGenotype, TSearchSpace, ICrossoverExecution<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}

public interface ICrossoverExecution<TGenotype> {
  TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random);
}

public abstract class CrossoverExecution<TGenotype, TSearchSpace, TProblem, TCrossover> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TCrossover>, ICrossoverExecution<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  protected CrossoverExecution(TCrossover parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) { }
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random);
}

public abstract class CrossoverExecution<TGenotype, TSearchSpace, TCrossover> : OperatorExecution<TGenotype, TSearchSpace, TCrossover>, ICrossoverExecution<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected CrossoverExecution(TCrossover parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random);
}

//
// public static class Crossover {
//   public static CustomCrossover<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TGenotype, TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> crossover)
//     where TSearchSpace : ISearchSpace<TGenotype>
//   {
//     return new CustomCrossover<TGenotype, TSearchSpace>(crossover);
//   }
// }
//
// public sealed class CustomCrossover<TGenotype, TSearchSpace> 
//   : ICrossover<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   private readonly Func<TGenotype, TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> crossover;
//   internal CustomCrossover(Func<TGenotype, TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> crossover) {
//     this.crossover = crossover;
//   }
//   public TGenotype Cross(TGenotype parent1, TGenotype parent2, TSearchSpace searchSpace, IRandomNumberGenerator random) => crossover(parent1, parent2, searchSpace, random);
// }
//
// public abstract class CrossoverBase<TGenotype, TSearchSpace> 
//   : ICrossover<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype> 
// {
//   public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TSearchSpace searchSpace, IRandomNumberGenerator random); 
// }

public sealed record ProblemSpecificCrossover<TGenotype, TSearchSpace, TProblem> : Crossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public Crossover<TGenotype, TSearchSpace> ProblemAgnosticCrossover { get; }

  public ProblemSpecificCrossover(Crossover<TGenotype, TSearchSpace> problemAgnosticCrossover) {
    ProblemAgnosticCrossover = problemAgnosticCrossover;
  }
  
  public override ICrossoverExecution<TGenotype> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return ProblemAgnosticCrossover.CreateExecution(searchSpace);
  }
}
