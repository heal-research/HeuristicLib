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
/// library, and runs once per validation, never during a run. An operator is checked when it declares an invariant
/// contract, and skipped otherwise.
/// </remarks>
public static class SearchConfigurationValidation
{
    /// <summary>
    /// Validates every operator reachable from <paramref name="configuration"/> against
    /// <paramref name="searchSpace"/>.
    /// </summary>
    public static ValidationReport Validate<TCandidate>(IExecutionInstanceResolvable configuration, ISearchSpace<TCandidate> searchSpace)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ValidationDiagnostic>();
        var visited = new HashSet<IExecutionInstanceResolvable>(ReferenceEqualityComparer.Instance);
        Walk(configuration, configuration.GetType().Name, searchSpace, visited, diagnostics);
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
