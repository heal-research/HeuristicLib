namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// The types an execution is built for: the search space and problem it operates over, and the search state it
/// produces.
/// </summary>
/// <remarks>
/// Built where an execution graph is created — by a run, or by validation asking what a run would do — and passed down
/// unchanged. A configuration reads the parts it is written about and ignores the rest, so an ordinary operator never
/// builds one.
/// <para>
/// A meta algorithm or an adapting operator that starts an <em>inner</em> execution over different types does build
/// one, for that inner execution, and passes it down in turn. Every signature belongs to exactly one execution, which
/// is why such a configuration must not forward the outer one to children that belong to the inner.
/// </para>
/// </remarks>
public readonly record struct ExecutionSignature(Type SearchSpace, Type Problem, Type SearchState)
{
    public static ExecutionSignature For<TSearchSpace, TProblem, TSearchState>() =>
        new(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState));

    /// <summary>
    /// Whether every one of <paramref name="configurations"/> fits this execution. An unset optional slot resolves to
    /// nothing, so a <see langword="null"/> entry is skipped rather than failing.
    /// </summary>
    /// <remarks>
    /// This is how a composition answers for the children it resolves under the signature it was given itself. A
    /// composition that resolves a child under different types — a different candidate, or a problem it adapts — must
    /// not use it, because those children belong to a different execution: answering for itself alone, which is the
    /// default, is what makes such a composition a boundary of the check. It may check them against the inner
    /// signature instead.
    /// </remarks>
    public bool Fits(params ReadOnlySpan<IExecutionInstanceResolvable?> configurations)
    {
        foreach (var configuration in configurations)
        {
            if (configuration is not null && !configuration.Fits(this))
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString() => Describe(SearchSpace, Problem, SearchState);

    /// <summary>
    /// Names a search space and problem, and a search state where the caller is written about one, in the wording the
    /// mismatch message below uses.
    /// </summary>
    public static string Describe(Type searchSpace, Type problem, Type? searchState = null) =>
        searchState is null
            ? $"{searchSpace.Name} with {problem.Name}"
            : $"{searchSpace.Name} with {problem.Name} producing {searchState.Name}";

    /// <summary>
    /// The exception an authoring base throws when it is asked for an execution it was not written for. One message
    /// for every role, so a mismatch reads the same wherever it is reported.
    /// </summary>
    public static InvalidOperationException Mismatch(object configuration, string writtenFor, string execution) =>
        new($"{configuration.GetType().Name} is written for {writtenFor}, and cannot run over {execution}.");
}
