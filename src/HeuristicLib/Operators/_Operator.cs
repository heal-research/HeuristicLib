using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators;

// public interface IOperator<TGenotype, in TSearchSpace, in TProblem>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TProblem : IOptimizable<TGenotype, TSearchSpace>
// {
// }

public abstract record class Operator<TGenotype, TSearchSpace, TProblem, TOperatorExecution>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public abstract TOperatorExecution CreateExecution(TSearchSpace searchSpace, TProblem problem);
}

public abstract record class Operator<TGenotype, TSearchSpace, TOperatorExecution>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public abstract TOperatorExecution CreateExecution(TSearchSpace searchSpace);
}

public abstract record class Operator<TOperatorExecution>
{
  public abstract TOperatorExecution CreateExecution();
}

// public interface IOperatorExecution<TGenotype, in TSearchSpace, in TProblem> {
//   
// }

public abstract class OperatorExecution<TGenotype, TSearchSpace, TProblem, TOperator> /*: IOperatorExecution<TGenotype, TSearchSpace, TProblem>*/
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  // where TOperator : IOperator<TGenotype, TSearchSpace, TProblem>
{
  public TOperator Parameters { get; }
  public TSearchSpace SearchSpace { get; }
  public TProblem Problem { get; }
  
  protected OperatorExecution(TOperator parameters, TSearchSpace searchSpace, TProblem problem) {
    Parameters = parameters;
    SearchSpace = searchSpace;
    Problem = problem;
  }
}

public abstract class OperatorExecution<TGenotype, TSearchSpace, TOperator> /*: IOperatorExecution<TGenotype, TSearchSpace, TProblem>*/
  where TSearchSpace : ISearchSpace<TGenotype>
  // where TProblem : IOptimizable<TGenotype, TSearchSpace>
// where TOperator : IOperator<TGenotype, TSearchSpace, TProblem>
{
  public TOperator Parameters { get; }
  public TSearchSpace SearchSpace { get; }
  // public TProblem Problem { get; }

  protected OperatorExecution(TOperator parameters, TSearchSpace searchSpace/*, TProblem problem*/) {
    Parameters = parameters;
    SearchSpace = searchSpace;
    // Problem = problem;
  }
}

public abstract class OperatorExecution<TOperator> /*: IOperatorExecution<TGenotype, TSearchSpace, TProblem>*/
// where TProblem : IOptimizable<TGenotype, TSearchSpace>
// where TOperator : IOperator<TGenotype, TSearchSpace, TProblem>
{
  public TOperator Parameters { get; }
  // public TProblem Problem { get; }

  protected OperatorExecution(TOperator parameters/*, TProblem problem*/) {
    Parameters = parameters;
    // Problem = problem;
  }
}


// public sealed record class ProblemSpecificOperator<TGenotype, TSearchSpace, TProblem, TOperatorExecution> : Operator<TGenotype, TSearchSpace, TProblem, TOperatorExecution>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TProblem : IOptimizable<TGenotype, TSearchSpace>
// {
//   public Operator<TGenotype, TSearchSpace, TOperatorExecution> ProblemAgnosticOperator { get; }
//
//   public ProblemSpecificOperator(Operator<TGenotype, TSearchSpace, TOperatorExecution> problemAgnosticOperator) {
//     ProblemAgnosticOperator = problemAgnosticOperator;
//   }
//   
//   public override TOperatorExecution CreateExecution(TProblem problem) {
//     return ProblemAgnosticOperator.CreateExecution();
//   }
//   
//   public static implicit operator ProblemSpecificOperator<TGenotype, TSearchSpace, TProblem, TOperatorExecution>(Operator<TGenotype, TSearchSpace, TOperatorExecution> problemAgnosticOperator) {
//     return new ProblemSpecificOperator<TGenotype, TSearchSpace, TProblem, TOperatorExecution>(problemAgnosticOperator);
//   }
// }
