namespace HEAL.HeuristicLib.Operators;

public interface IOperator {
}

public interface IOperatorTemplate<out TOperator, in TParameters>
  where TOperator : IOperator {
  TOperator Parameterize(TParameters parameters);
}
