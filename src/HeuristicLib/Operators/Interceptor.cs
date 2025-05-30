using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Interceptor<TGenotype, TSearchSpace, TProblem, TState> : Operator<TGenotype, TSearchSpace, TProblem, IInterceptorExecution<TState>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Interceptor<TGenotype, TSearchSpace, TProblem, TState>?(Interceptor<TGenotype, TSearchSpace, TState>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificInterceptor<TGenotype, TSearchSpace, TProblem, TState>(problemAgnosticOperator);
  }
}

public abstract record class Interceptor<TGenotype, TSearchSpace, TState> : Operator<TGenotype, TSearchSpace, IInterceptorExecution<TState>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}
  
public interface IInterceptorExecution<TState> {
  TState Transform(TState state);
}

public abstract class InterceptorExecution<TGenotype, TSearchSpace, TProblem, TState, TInterceptor> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TInterceptor>, IInterceptorExecution<TState> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>

{
  protected InterceptorExecution(TInterceptor parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) { }
  public abstract TState Transform(TState state);
}

public abstract class InterceptorExecution<TGenotype, TSearchSpace, TState, TInterceptor> : OperatorExecution<TGenotype, TSearchSpace, TInterceptor>, IInterceptorExecution<TState> 
  where TSearchSpace : ISearchSpace<TGenotype>

{
  protected InterceptorExecution(TInterceptor parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public abstract TState Transform(TState state);
}

public sealed record ProblemSpecificInterceptor<TGenotype, TSearchSpace, TProblem, TState> : Interceptor<TGenotype, TSearchSpace, TProblem, TState>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public Interceptor<TGenotype, TSearchSpace, TState> ProblemAgnosticInterceptor { get; }

  public ProblemSpecificInterceptor(Interceptor<TGenotype, TSearchSpace, TState> problemAgnosticInterceptor) {
    ProblemAgnosticInterceptor = problemAgnosticInterceptor;
  }

  public override IInterceptorExecution<TState> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return ProblemAgnosticInterceptor.CreateExecution(searchSpace);
  }
}


public static class Interceptors {
  public static IdentityInterceptor<TGenotype, TSearchSpace, TState> Identity<TGenotype, TSearchSpace, TState>()
    where TSearchSpace : ISearchSpace<TGenotype>
  {
    return new IdentityInterceptor<TGenotype, TSearchSpace, TState>();
  }
  // public static Interceptor<TState> Create<TState>(Func<TState, TState> transform)/* where TState : IResultState*/ {
  //   return new CustomInterceptor<TState>(transform);
  // }
}

// public class CustomInterceptor<TState> : Interceptor<TState> /*where TState : IResultState*/ {
//   private readonly Func<TState, TState> transform;
//   internal CustomInterceptor(Func<TState, TState> transform) {
//     this.transform = transform;
//   }
//   public TState Transform(TState state) => transform(state);
// }



public record class IdentityInterceptor<TGenotype, TSearchSpace, TState> : Interceptor<TGenotype, TSearchSpace, TState> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public override IdentityInterceptorExecution<TGenotype, TSearchSpace, TState> CreateExecution(TSearchSpace searchSpace) {
    return new IdentityInterceptorExecution<TGenotype, TSearchSpace, TState>(this, searchSpace);
  }
}

public class IdentityInterceptorExecution<TGenotype, TSearchSpace, TState> : InterceptorExecution<TGenotype, TSearchSpace, TState, IdentityInterceptor<TGenotype, TSearchSpace, TState>> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public IdentityInterceptorExecution(IdentityInterceptor<TGenotype, TSearchSpace, TState> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override TState Transform(TState state) => state;
}
