namespace HEAL.HeuristicLib.Operators;

public abstract record class Interceptor<TState> : Operator<IInterceptorInstance<TState>> {
}
  
public interface IInterceptorInstance<TState> {
  TState Transform(TState state);
}

public abstract class InterceptorInstance<TState, TInterceptor> : OperatorInstance<TInterceptor>, IInterceptorInstance<TState> {
  protected InterceptorInstance(TInterceptor parameters) : base(parameters) { }
  public abstract TState Transform(TState state);
}

public static class Interceptors {
  public static IdentityInterceptor<TState> Identity<TState>() /*where TState : IResultState*/ => new IdentityInterceptor<TState>();
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



public record class IdentityInterceptor<TState> : Interceptor<TState> {
  public override IInterceptorInstance<TState> CreateInstance() {
    return new IdentityInterceptorInstance<TState>(this);
  }
}

public class IdentityInterceptorInstance<TState> : InterceptorInstance<TState, IdentityInterceptor<TState>> {
  public IdentityInterceptorInstance(IdentityInterceptor<TState> parameters) : base(parameters) { }
  public override TState Transform(TState state) => state;
}
