using System.Reflection;
using System.Text.RegularExpressions;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests.Docs.Reference;

/// <summary>
/// Keeps the reference catalogs complete: every public algorithm and operator of the core package has a row, and every
/// row names a type that exists.
/// </summary>
/// <remarks>
/// Operators that wrap another operator of any operation, such as <c>Named</c> or <c>Counted</c>, belong to the
/// composition and analysis pages and are left out.
/// </remarks>
public sealed partial class ReferenceCatalogDocumentationSpecs
{
    private static readonly Assembly Core = typeof(IOperator).Assembly;

    [Fact]
    public void TheOperatorCatalogListsEveryOperator()
    {
        var operators = Core.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IOperator).IsAssignableFrom(type))
            .Where(type => type.Namespace != "HEAL.HeuristicLib.Inspection" && !IsOperationGeneric(type))
            .Select(Name);

        ShouldListExactly("operators.md", operators);
    }

    [Fact]
    public void TheAlgorithmCatalogListsEveryAlgorithm()
    {
        var algorithms = Core.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && ImplementsAlgorithm(type))
            .Where(type => type.Namespace != "HEAL.HeuristicLib.Inspection")
            .Select(Name);

        ShouldListExactly("algorithms.md", algorithms, rejectUnknownRows: false);
    }

    private static void ShouldListExactly(string page, IEnumerable<string> types, bool rejectUnknownRows = true)
    {
        var path = Path.Combine(FigureData.RepositoryRoot(), "docs", "reference", page);
        var listed = File.ReadLines(path)
            .Select(line => CatalogRow().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .ToHashSet();
        var expected = types.ToHashSet();

        expected.Except(listed).Order().ShouldBeEmpty($"{page} misses these types");
        if (rejectUnknownRows)
        {
            listed.Except(expected).Order().ShouldBeEmpty($"{page} lists types that do not exist");
        }
    }

    private static bool IsOperationGeneric(Type type)
    {
        return type.GetGenericArguments()
            .SelectMany(argument => argument.GetGenericParameterConstraints())
            .Any(constraint => IsOperationContract(constraint) || constraint.GetInterfaces().Any(IsOperationContract));
    }

    private static bool IsOperationContract(Type contract)
    {
        return contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IOperation<>);
    }

    private static bool ImplementsAlgorithm(Type type)
    {
        return type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IAlgorithm<>));
    }

    private static string Name(Type type)
    {
        var tick = type.Name.IndexOf('`');
        return tick < 0 ? type.Name : type.Name[..tick];
    }

    [GeneratedRegex(@"^\| `(?<name>[A-Za-z0-9]+)(<[^`]*>)?`")]
    private static partial Regex CatalogRow();
}
