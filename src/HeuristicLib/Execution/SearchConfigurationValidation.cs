using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

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
/// The walk goes over declared parameters, so it reaches what a configuration declares, including configurations written
/// outside the library, and it runs once per validation, never during a run.
/// <para>
/// Two independent questions are asked of each node. Whether it declares an invariant contract the search space
/// contradicts, which is skipped when it declares none; and, when the run's types are supplied, whether it was
/// written for them at all. The second is the same <see cref="ExecutionSignature.Supports(IConfigurationNode)"/> resolution
/// asks before it builds, so validation and execution decide by one rule. Because the walk asks each
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
    public static ValidationReport Validate<TCandidate>(IConfigurationNode configuration, ISearchSpace<TCandidate> searchSpace)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ValidationDiagnostic>();
        Walk(configuration, configuration.GetType().Name, searchSpace, new HashSet<IConfigurationNode>(ReferenceEqualityComparer.Instance), diagnostics);
        return new ValidationReport(diagnostics.ToImmutable());
    }

    /// <summary>
    /// Validates every operator reachable from <paramref name="configuration"/> against
    /// <paramref name="searchSpace"/>, and every configuration in the graph against
    /// <paramref name="execution"/>.
    /// </summary>
    public static ValidationReport Validate<TCandidate>(IConfigurationNode configuration, ISearchSpace<TCandidate> searchSpace, ExecutionSignature execution)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ValidationDiagnostic>();
        var rootPath = configuration.GetType().Name;

        ReportMismatch(configuration, rootPath, execution, diagnostics);
        Walk(configuration, rootPath, searchSpace, new HashSet<IConfigurationNode>(ReferenceEqualityComparer.Instance), diagnostics);
        return new ValidationReport(diagnostics.ToImmutable());
    }

    /// <summary>
    /// The failure resolution reports when <paramref name="configuration"/> does not support <paramref name="execution"/>,
    /// listing the configurations responsible rather than only the one being resolved.
    /// </summary>
    internal static InvalidOperationException Mismatch(IConfigurationNode configuration, ExecutionSignature execution)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ValidationDiagnostic>();
        ReportMismatch(configuration, ExecutionSignature.Name(configuration.GetType()), execution, diagnostics);
        return new InvalidOperationException(
            $"{ExecutionSignature.Name(configuration.GetType())} cannot run over {execution}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"  - {diagnostic}")));
    }

    /// <summary>
    /// Returns <paramref name="source"/> as an algorithm producing <typeparamref name="TSearchState"/>, or throws when it
    /// does not produce that state or when it or a declared child does not support <paramref name="execution"/>.
    /// </summary>
    internal static IAlgorithm<TCandidate, TSearchState> RequireSupport<TCandidate, TSearchState>(IAlgorithm<TCandidate> source, ExecutionSignature execution)
        where TSearchState : class, ISearchState
    {
        if (source is not IAlgorithm<TCandidate, TSearchState> algorithm)
            throw new InvalidOperationException($"{ExecutionSignature.Name(source.GetType())} produces {ProducedSearchStates(source)}, " +
                $"and cannot run producing {ExecutionSignature.Name(typeof(TSearchState))}.");
        if (!execution.Supports(algorithm))
            throw Mismatch(algorithm, execution);
        return algorithm;
    }

    /// <summary>Throws when <paramref name="source"/> or a declared child does not support <paramref name="execution"/>.</summary>
    /// <remarks>When the operator itself is written for the execution, a child is responsible, so the failure names the child.</remarks>
    internal static void RequireSupport<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, ExecutionSignature execution)
        where TOperation : IOperation<TOperation>
    {
        if (execution.Supports(source))
            return;
        if (source.Supports(execution))
            throw Mismatch(source, execution);
        throw ExecutionSignature.Mismatch(source,
            source is Operator<TOperation, TCandidate> authored ? authored.SupportedExecutionDescription : "its declared signature",
            execution.ToString());
    }

    // Reflection only to word the exception.
    private static string ProducedSearchStates(IConfigurationNode source) =>
        string.Join(" or ", source.GetType().GetInterfaces()
            .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IAlgorithm<,>))
            .Select(contract => ExecutionSignature.Name(contract.GetGenericArguments()[1]))
            .DefaultIfEmpty("no search state"));

    /// <summary>
    /// Reports the node and everything below it, returning the invariants already reported in this subtree.
    /// </summary>
    /// <remarks>
    /// Children are visited first, and a composition's own failure is suppressed when a descendant already reported
    /// the same invariant. A composed operator inherits its children's limits, so repeating them would bury the one
    /// diagnostic that names the operator a user has to change.
    /// </remarks>
    private static HashSet<string> Walk<TCandidate>(IConfigurationNode node, string path, ISearchSpace<TCandidate> searchSpace, HashSet<IConfigurationNode> visited, ImmutableArray<ValidationDiagnostic>.Builder diagnostics)
    {
        var reported = new HashSet<string>(StringComparer.Ordinal);
        if (!visited.Add(node))
        {
            return reported;
        }

        foreach (var (child, childPath, _) in Children(node, path))
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
    /// A subtree that answers yes is not descended into at all. Each child is asked about the execution its configuration
    /// and then its parameter hand it, so a child is reported against the same types it receives during construction.
    /// </remarks>
    private static bool ReportMismatch(IConfigurationNode node, string path, ExecutionSignature execution, ImmutableArray<ValidationDiagnostic>.Builder diagnostics)
    {
        if (execution.Supports(node))
        {
            return false;
        }

        var blamedAChild = false;
        foreach (var (child, childPath, parameter) in Children(node, path))
        {
            blamedAChild |= ReportMismatch(child, childPath, parameter.ChildExecution(node.ChildExecution(execution)), diagnostics);
        }

        if (!blamedAChild)
        {
            diagnostics.Add(new ValidationDiagnostic(path, $"{node.GetType().Name} was not written for an execution over {execution}."));
        }

        return true;
    }

    /// <summary>
    /// The configurations a node's parameters hold or name, each with its path and the parameter that declares it.
    /// </summary>
    private static List<(IConfigurationNode Child, string Path, IParameter Parameter)> Children(IConfigurationNode node, string path)
    {
        var found = new List<(IConfigurationNode, string, IParameter)>();
        foreach (var parameter in node.GetParameters())
            parameter.Rewrite(node, new ChildCollector(found, path, parameter));
        return found;
    }

    // Visits each slot of one parameter and leaves it as it is, so the walk reuses what a substitution sees.
    private sealed class ChildCollector(List<(IConfigurationNode, string, IParameter)> found, string path, IParameter parameter) : ISlotRewriter
    {
        public IConfigurationNode Slot(IConfigurationNode owner, string name, Type slotType, IConfigurationNode child)
        {
            found.Add((child, $"{path}.{name}", parameter));
            return child;
        }

        public IConfigurationNode Reference(string name, IConfigurationNode entry)
        {
            found.Add((entry, $"{path}.{name}", parameter));
            return entry;
        }

        public void Boundary(IConfigurationNode owner, string name)
        {
        }
    }
}
