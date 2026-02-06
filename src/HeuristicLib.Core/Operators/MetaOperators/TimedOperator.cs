namespace HEAL.HeuristicLib.Operators.MetaOperators;

public sealed class InvocationTiming
{
  private TimeSpan totalTime = TimeSpan.Zero;
  public TimeSpan TotalTime => totalTime;
  
  public void AddTime(TimeSpan time)
  {
    totalTime += time;
  }
}

// public class TimedOperator<TInput, TContext, TOutput>
//   : IOperator<TInput, TContext, TOutput>
// {
//   private readonly IOperator<TInput, TContext, TOutput> @operator;
//   private readonly InvocationTiming timing;
//
//   public TimedOperator(IOperator<TInput, TContext, TOutput> @operator, InvocationTiming timing)
//   {
//     this.@operator = @operator;
//     this.timing = timing;
//   }
//
//
//   public TOutput Execute(TInput input, TContext context)
//   {
//     // ToDo: think if we want the actual timer as dependency
//     var start = Stopwatch.GetTimestamp();
//     var result = @operator.Execute(input, context);
//     var duration = Stopwatch.GetElapsedTime(start);
//     timing.AddTime(duration);
//     return result;
//   }
// }
