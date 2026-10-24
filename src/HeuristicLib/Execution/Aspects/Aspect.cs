using System.Collections;

namespace HEAL.HeuristicLib.Execution;

/// <summary>The list of pointcut/advice pairs woven into the executions they select.</summary>
/// <remarks>
/// Derived constructors assign <see cref="Advices"/>, and an aspect that builds on others lists their pairs, as in
/// <c>Advices = [.. Aspect.Observe(evaluator.Calls, Record)]</c>. Each advice instance pairs with one pointcut; applying
/// the same behavior at two pointcuts needs two instances, which may share one result object. A resolution scope applies
/// an advice instance once, at its first occurrence, and keeps its prepared state per scope state.
/// </remarks>
public abstract class Aspect : IReadOnlyList<(Pointcut Pointcut, Advice Advice)>
{
    /// <summary>The pointcut/advice pairs in application order; earlier advice wraps closer to the source execution.</summary>
    protected ImmutableArray<(Pointcut Pointcut, Advice Advice)> Advices { get; init; } = [];

    public int Count => Advices.Length;

    public (Pointcut Pointcut, Advice Advice) this[int index] => Advices[index];

    public ImmutableArray<(Pointcut Pointcut, Advice Advice)>.Enumerator GetEnumerator() => Advices.GetEnumerator();

    IEnumerator<(Pointcut Pointcut, Advice Advice)> IEnumerable<(Pointcut Pointcut, Advice Advice)>.GetEnumerator() =>
        ((IEnumerable<(Pointcut Pointcut, Advice Advice)>)Advices).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Advices).GetEnumerator();

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
