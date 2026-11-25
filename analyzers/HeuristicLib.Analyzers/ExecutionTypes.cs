using Microsoft.CodeAnalysis;

namespace HEAL.HeuristicLib.Analyzers;

/// <summary>Recognizes the library's execution types for the analyzers and their code fixes.</summary>
internal static class ExecutionTypes
{
    private const string Namespace = "HEAL.HeuristicLib.Execution";

    /// <summary>Returns whether the type is one of the named types of the execution namespace.</summary>
    /// <remarks>Names are simple names, so a constructed generic type matches by its definition's name.</remarks>
    internal static bool Is(ITypeSymbol? type, params string[] names) =>
        type is not null && names.Contains(type.Name) && type.ContainingNamespace?.ToDisplayString() == Namespace;
}
