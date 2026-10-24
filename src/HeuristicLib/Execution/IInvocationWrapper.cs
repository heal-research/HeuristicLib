using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Adds behavior, such as timing, counting or retry, around calls to an executor's operation method.</summary>
/// <remarks>
/// An operation builds the wrapper into the executor its <see cref="IOperation{TSelf}"/> members wrap; each call to that
/// executor's named method, such as Mutate, passes one invocation to <see cref="Invoke{TInvocation, TResult}"/>.
/// Implement wrappers as small immutable structs so that each call is compiled for the wrapper type. Keep data that
/// outlives a call, such as a counter, in a referenced object. The wrapping executor forwards the wrapped executor's
/// controls unless the wrapper implements <see cref="IExecutorControlProvider"/>, which then supplies them.
/// </remarks>
public interface IInvocationWrapper
{
    /// <summary>Performs one invocation, calling <see cref="IInvocation{TResult}.Proceed"/> to run the wrapped method.</summary>
    TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>;
}
