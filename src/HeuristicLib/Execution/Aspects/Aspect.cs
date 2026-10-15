namespace HEAL.HeuristicLib.Execution;

/// <summary>Pairs pointcuts with the advice woven into the executions they select.</summary>
/// <remarks>
/// Derived constructors assign <see cref="Advices"/>. Each advice instance pairs with one pointcut; applying the same
/// behavior at two pointcuts needs two instances, which may share one result object. A resolution scope applies an
/// advice instance once, at its first occurrence, and keeps its prepared state per scope state.
/// </remarks>
public abstract class Aspect
{
    /// <summary>The pointcut/advice pairs in application order; earlier advice wraps closer to the source execution.</summary>
    public ImmutableArray<(Pointcut Pointcut, Advice Advice)> Advices { get; protected init; } = [];

    /// <remarks>Each call creates an independent aspect, even for the same pointcut and advice.</remarks>
    public static Aspect Create<TSelected, TConfiguration>(Pointcut<TSelected> pointcut, Advice<TConfiguration> advice)
        where TSelected : class, TConfiguration
        where TConfiguration : class, IConfigurationNode => new AdvisingAspect(pointcut, advice);

    private sealed class AdvisingAspect : Aspect
    {
        public AdvisingAspect(Pointcut pointcut, Advice advice)
        {
            Advices = [(pointcut, advice)];
        }
    }
}
