using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MetaOperators;

public sealed class InvocationCounter
{
  private int currentCount = 0;

  public int CurrentCount => currentCount;
  
  public void Increment(int by = 1)
  {
    Interlocked.Add(ref currentCount, by);
  }
}

public class CountedOperator<TInput, TContext, TOutput> : IOperator<TInput, TContext, TOutput>
{
  private readonly IOperator<TInput, TContext, TOutput> @operator;
  private readonly InvocationCounter counter;

  public CountedOperator(IOperator<TInput, TContext, TOutput> @operator, InvocationCounter counter)
  {
    this.@operator = @operator;
    this.counter = counter;
  }

  public TOutput Apply(TInput input, TContext context)
  {
    var result = @operator.Apply(input, context);
    // ToDo: figure out the internal counter?
    counter.Increment();
    return result;
  }
}

public static class CountedOperator
{
  // Maybe move to Mutator namespace?
  extension<TInput, TContext, TOutput>(IOperator<TInput, TContext, TOutput> @operator)
  {
    public CountedOperator<TInput, TContext, TOutput> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return @operator.CountInvocations(counter);
    }
    
    public CountedOperator<TInput, TContext, TOutput> CountInvocations(InvocationCounter counter)
    {
      return new CountedOperator<TInput, TContext, TOutput>(@operator, counter);
    }
  }
  
  // ToDo: think if we really want this to avoid combinatorial explosion of extension methods
  extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IMutator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return mutator.CountInvocations(counter);
    }
    
    public IMutator<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      var @operator = mutator.AsOperator().CountInvocations(counter);
      return @operator.AsMutator();
    }
  }
}

