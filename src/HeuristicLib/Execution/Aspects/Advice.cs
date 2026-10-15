namespace HEAL.HeuristicLib.Execution;

/// <summary>Wraps the executions built for the configurations its pointcut selects.</summary>
/// <remarks>
/// Each advised role has one abstract advice base whose wrapper factory is generic over the run's search space and
/// problem. Resolution compares advice by reference, and each instance keys its own state.
/// </remarks>
public abstract class Advice
{
    private protected Advice() { }
}

/// <summary>Advice that attaches to configurations of the role <typeparamref name="TConfiguration"/>.</summary>
/// <remarks>The role lets the compiler reject a pointcut and advice for different roles; it does not change what is wrapped.</remarks>
public abstract class Advice<TConfiguration> : Advice
    where TConfiguration : class, IConfigurationNode;
