namespace HEAL.HeuristicLib.Operators;

public abstract record class Operator<TOperatorInstance> {
  public abstract TOperatorInstance CreateInstance();
}

public abstract class OperatorInstance<TOperator> {
  public TOperator Parameters { get; }
  
  protected OperatorInstance(TOperator parameters) {
    Parameters = parameters;
  }
}
