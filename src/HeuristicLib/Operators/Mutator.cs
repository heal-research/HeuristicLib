using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Mutator<TGenotype, TSearchSpace, TProblem> : Operator<TGenotype, TSearchSpace, TProblem, IMutatorExecution<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Mutator<TGenotype, TSearchSpace, TProblem>?(Mutator<TGenotype, TSearchSpace>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificMutator<TGenotype, TSearchSpace, TProblem>(problemAgnosticOperator);
  }
}

public abstract record class Mutator<TGenotype, TSearchSpace> : Operator<TGenotype, TSearchSpace, IMutatorExecution<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}

public interface IMutatorExecution<TGenotype> {
  TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
}

public abstract class MutatorExecution<TGenotype, TSearchSpace, TProblem, TMutator> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TMutator>, IMutatorExecution<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  protected MutatorExecution(TMutator parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) { }
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
}

public abstract class MutatorExecution<TGenotype, TSearchSpace, TMutator> : OperatorExecution<TGenotype, TSearchSpace, TMutator>, IMutatorExecution<TGenotype>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected MutatorExecution(TMutator parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
}


//
// public static class Mutator {
//   public static CustomMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> mutator) 
//     where TSearchSpace : ISearchSpace<TGenotype>
//   {
//     return new CustomMutator<TGenotype, TSearchSpace>(mutator);
//   }
// }
//
// public sealed class CustomMutator<TGenotype, TSearchSpace> 
//   : IMutator<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   private readonly Func<TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> mutator;
//   internal CustomMutator(Func<TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> mutator) {
//     this.mutator = mutator;
//   }
//   public TGenotype Mutate(TGenotype parent, TSearchSpace searchSpace, IRandomNumberGenerator random) => mutator(parent, searchSpace, random);
// }
//
// public abstract class MutatorBase<TGenotype, TSearchSpace>
//   : IMutator<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   public abstract TGenotype Mutate(TGenotype parent, TSearchSpace searchSpace, IRandomNumberGenerator random);
// }

public sealed record class ProblemSpecificMutator<TGenotype, TSearchSpace, TProblem> : Mutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  private readonly Mutator<TGenotype, TSearchSpace> problemAgnosticOperator;

  public ProblemSpecificMutator(Mutator<TGenotype, TSearchSpace> problemAgnosticOperator) {
    this.problemAgnosticOperator = problemAgnosticOperator;
  }

  public override IMutatorExecution<TGenotype> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return problemAgnosticOperator.CreateExecution(searchSpace);
  }
}
