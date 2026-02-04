namespace HEAL.HeuristicLib.Operators.MetaOperators;

public interface IOperatorObservation<in TInput, in TOutput>
{
  void OnExecuted(TInput input, TOutput output);
}

public class ObservedOperator<TInput, TContext, TOutput>
  : IOperator<TInput, TContext, TOutput>
{
  private readonly IOperator<TInput, TContext, TOutput> @operator;
  private readonly IReadOnlyList<IOperatorObservation<TInput, TOutput>> observers;


  public ObservedOperator(IOperator<TInput, TContext, TOutput> @operator, params IReadOnlyList<IOperatorObservation<TInput, TOutput>> observers)
  {
    this.@operator = @operator;
    this.observers = observers;
  }
  
  public TOutput Execute(TInput input, TContext context)
  {
    var output = @operator.Execute(input, context);
    
    foreach (var observer in observers) {
      observer.OnExecuted(input, output);
    }
    return output;
  }
}

public static class ObservedOperatorExtensions
{
  extension<TInput, TContext, TOutput>(IOperator<TInput, TContext, TOutput> @operator) {
    public ObservedOperator<TInput, TContext, TOutput> ObserveWith(params IOperatorObservation<TInput, TOutput>[] observers)
    {
      return new ObservedOperator<TInput, TContext, TOutput>(@operator, observers);
    }
    
    public ObservedOperator<TInput, TContext, TOutput> ObserveWith(Action<TInput, TOutput> action)
    {
      var funcObservation = new FuncObservation<TInput, TOutput>(action);
      return new ObservedOperator<TInput, TContext, TOutput>(@operator, funcObservation);
    }
  }
}

public class FuncObservation<TInput, TOutput> : IOperatorObservation<TInput, TOutput>
{
  private readonly Action<TInput, TOutput> action;

  public FuncObservation(Action<TInput, TOutput> action)
  {
    this.action = action;
  }
  
  public void OnExecuted(TInput input, TOutput output)
  {
    action(input, output);
  }
}
