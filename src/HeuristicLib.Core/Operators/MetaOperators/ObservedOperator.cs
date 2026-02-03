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
