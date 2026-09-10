using System.Reflection;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// One problem found while validating a configuration, naming where in the configuration it was found.
/// </summary>
public sealed record ValidationDiagnostic(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

/// <summary>
/// Everything a validation pass found. An empty report means the configuration is usable over the search space it was
/// checked against.
/// </summary>
public sealed record ValidationReport(ImmutableArray<ValidationDiagnostic> Diagnostics)
{
    public static ValidationReport Valid { get; } = new([]);

    public bool IsValid => Diagnostics.Length == 0;

    /// <summary>
    /// Throws when the configuration is not usable, listing every problem rather than only the first.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (IsValid)
        {
            return;
        }

        throw new InvalidOperationException(
            $"The configuration cannot be used over this search space:{Environment.NewLine}" +
            string.Join(Environment.NewLine, Diagnostics.Select(diagnostic => $"  - {diagnostic}")));
    }

    public override string ToString() =>
        IsValid ? "valid" : string.Join(Environment.NewLine, Diagnostics.Select(diagnostic => diagnostic.ToString()));
}

/// <summary>
/// Checks a configuration graph against a search space before a run starts, so an incompatible operator is reported
/// up front rather than when the algorithm first reaches it.
/// </summary>
/// <remarks>
/// The walk is by reflection over configuration properties, so it covers algorithms and operators written outside the
/// library, and runs once per validation, never during a run.
/// <para>
/// Two independent questions are asked of each node. Whether it declares an invariant contract the search space
/// contradicts, which is skipped when it declares none; and, when the run's types are supplied, whether it was
/// written for them at all. The second is the same <see cref="IExecutionInstanceResolvable.Fits"/> the
/// authoring bases apply when they bridge, so validation and execution decide by one rule. Because the walk asks each
/// node directly rather than building anything, it also reaches children a run would only resolve later, such as the
/// stages inside a cycling algorithm.
/// </para>
/// </remarks>
public static class SearchConfigurationValidation
{
    /// <summary>
    /// Validates every operator reachable from <paramref name="configuration"/> against
    /// <paramref name="searchSpace"/>, checking declared invariants only.
    /// </summary>
    /// <remarks>
    /// The form to use for one operator or composition on its own, where there is no problem and no search state to
    /// name. Use the overload taking an <see cref="ExecutionSignature"/> where all three are known; it adds the check
    /// that each configuration was written for that execution.
    /// </remarks>
    public static ValidationReport Validate<TCandidate>(IExecutionInstanceResolvable configuration, ISearchSpace<TCandidate> searchSpace)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ValidationDiagnostic>();
        Walk(configuration, configuration.GetType().Name, searchSpace, new HashSet<IExecutionInstanceResolvable>(ReferenceEqualityComparer.Instance), diagnostics);
        return new ValidationReport(diagnostics.ToImmutable());
    }

    /// <summary>
    /// Validates every operator reachable from <paramref name="configuration"/> against
    /// <paramref name="searchSpace"/>, and every configuration in the graph against
    /// <paramref name="execution"/>.
    /// </summary>
    public static ValidationReport Validate<TCandidate>(IExecutionInstanceResolvable configuration, ISearchSpace<TCandidate> searchSpace, ExecutionSignature execution)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ValidationDiagnostic>();
        var rootPath = configuration.GetType().Name;

        ReportMismatch(configuration, rootPath, execution, diagnostics);
        Walk(configuration, rootPath, searchSpace, new HashSet<IExecutionInstanceResolvable>(ReferenceEqualityComparer.Instance), diagnostics);
        return new ValidationReport(diagnostics.ToImmutable());
    }

    /// <summary>
    /// Reports the node and everything below it, returning the invariants already reported in this subtree.
    /// </summary>
    /// <remarks>
    /// Children are visited first, and a composition's own failure is suppressed when a descendant already reported
    /// the same invariant. A composed operator inherits its children's limits, so repeating them would bury the one
    /// diagnostic that names the operator a user has to change.
    /// </remarks>
    private static HashSet<string> Walk<TCandidate>(IExecutionInstanceResolvable node, string path, ISearchSpace<TCandidate> searchSpace, HashSet<IExecutionInstanceResolvable> visited, ImmutableArray<ValidationDiagnostic>.Builder diagnostics)
    {
        var reported = new HashSet<string>(StringComparer.Ordinal);
        if (!visited.Add(node))
        {
            return reported;
        }

        foreach (var (child, childPath) in Children(node, path))
        {
            reported.UnionWith(Walk(child, childPath, searchSpace, visited, diagnostics));
        }

        if (node is not IOperator candidateOperator)
        {
            return reported;
        }

        var incompatibilities = SearchSpaceCompatibility.Check(candidateOperator, searchSpace);
        foreach (var incompatibility in incompatibilities)
        {
            if (!reported.Contains(incompatibility.InvariantName))
            {
                diagnostics.Add(new ValidationDiagnostic(path, incompatibility.Explanation));
            }
        }

        foreach (var incompatibility in incompatibilities)
        {
            reported.Add(incompatibility.InvariantName);
        }

        return reported;
    }

    /// <summary>
    /// Reports the node or nodes responsible when <paramref name="node"/> was not written for
    /// <paramref name="execution"/>, and returns whether it was responsible at all.
    /// </summary>
    /// <remarks>
    /// A composition that passes the run to its children answers for its whole subtree, so a subtree that answers yes
    /// is not descended into at all. Descending only into a subtree that answered no is also what keeps a composition
    /// which adapts its children out of the report: such a composition does not forward, so it answers for itself, and
    /// its children are never asked about a run they were never going to see.
    /// </remarks>
    private static bool ReportMismatch(IExecutionInstanceResolvable node, string path, ExecutionSignature execution, ImmutableArray<ValidationDiagnostic>.Builder diagnostics)
    {
        if (node.Fits(execution))
        {
            return false;
        }

        var blamedAChild = false;
        foreach (var (child, childPath) in Children(node, path))
        {
            blamedAChild |= ReportMismatch(child, childPath, execution, diagnostics);
        }

        if (!blamedAChild)
        {
            diagnostics.Add(new ValidationDiagnostic(path, $"{node.GetType().Name} was not written for an execution over {execution}."));
        }

        return true;
    }

    /// <summary>
    /// Yields the configuration children of a node: any property value, or element of an enumerable property, that is
    /// itself a configuration.
    /// </summary>
    private static IEnumerable<(IExecutionInstanceResolvable Child, string Path)> Children(IExecutionInstanceResolvable node, string path)
    {
        foreach (var property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (value is null)
            {
                continue;
            }

            if (value is IExecutionInstanceResolvable child)
            {
                yield return (child, $"{path}.{property.Name}");
                continue;
            }

            if (value is not System.Collections.IEnumerable elements || value is string)
            {
                continue;
            }

            var index = 0;
            foreach (var element in elements)
            {
                if (element is IExecutionInstanceResolvable nestedChild)
                {
                    yield return (nestedChild, $"{path}.{property.Name}[{index}]");
                }

                index++;
            }
        }
    }
}
