using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Execution;

/// <summary>The value and candidate-work count returned by one successful operation call.</summary>
public readonly record struct OperationCallResult<TResult>(TResult Value, int CandidateWork);

/// <summary>Chooses which child to call, or repeats a child call, through an operation's typed continuation.</summary>
/// <remarks>Arguments and results retain their operation-specific types. The random source is null when the operation has no RNG input. Policies must not replace or rewind a supplied generator.</remarks>
public abstract class SynchronousPolicy
{
    public abstract OperationCallResult<TResult> Execute<TExecutor, TArguments, TResult>(
        ImmutableArray<TExecutor> children, TArguments arguments, IRandomNumberGenerator? random,
        Func<TExecutor, TArguments, OperationCallResult<TResult>> next);
}
