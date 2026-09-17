using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Analyzers.CodeFixes;
using HEAL.HeuristicLib.Operators.Mutators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>
/// The fix is offered for a rule reported as an error, so code that does not compile leaves the author worse off than
/// no fix at all. These specs apply it and compile the result.
/// </summary>
public class CreateExecutionInstanceCodeFixTests
{
    private const string Preamble = """
      using System.Collections.Generic;
      using HEAL.HeuristicLib.Execution;
      using HEAL.HeuristicLib.Operators;
      using HEAL.HeuristicLib.Operators.Mutators;
      using HEAL.HeuristicLib.Problems;
      using HEAL.HeuristicLib.Random;
      using HEAL.HeuristicLib.SearchSpaces;
      using SS = HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>;
      using P = HEAL.HeuristicLib.Problems.IProblem<int, HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>>;

      """;

    /// <remarks>
    /// The shape every migrated role has: the child is held behind its interface, so the bypassed call is generic and
    /// the replacement needs the three arguments none of which the operator alone can supply.
    /// </remarks>
    [Fact]
    public async Task FixedCode_Compiles_WhenTheBypassedCallIsGeneric()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
          file sealed record BypassingMutator : Mutator<int>
          {
              public required IMutator<int> Child { get; init; }

              public override IMutatorInstance<int, SS, P> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
                  Child.CreateExecutionInstance<SS, P>(instanceRegistry);
          }
          """);

        fixedSource.ShouldContain("instanceRegistry.Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    /// <remarks>The bound base declares a non-generic creation method, so the arguments come from its return type.</remarks>
    [Fact]
    public async Task FixedCode_Compiles_WhenTheBypassedCallIsNonGeneric()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
          file sealed record ChildMutator : Mutator<int>
          {
              public override IMutatorInstance<int, SS, P> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
                  new Instance();

              private sealed class Instance : MutatorInstance<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }

          file sealed record BypassingMutator : Mutator<int>
          {
              public required ChildMutator Child { get; init; }

              public override IMutatorInstance<int, SS, P> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
                  Child.CreateExecutionInstance(instanceRegistry);
          }
          """);

        fixedSource.ShouldContain("instanceRegistry.Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    private static async Task<string> ApplyFixAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace
            .AddProject("CodeFixTest", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))
            .AddMetadataReferences(References);

        var document = project.AddDocument("Input.cs", SourceText.From(source));
        var compilation = await document.Project.GetCompilationAsync();
        compilation.ShouldNotBeNull();

        var diagnostics = await compilation
            .WithAnalyzers([new CreateExecutionInstanceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.Single(d => d.Id == CreateExecutionInstanceAnalyzer.DiagnosticId);

        CodeAction? registered = null;
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => registered = action,
            CancellationToken.None);

        await new CreateExecutionInstanceCodeFixProvider().RegisterCodeFixesAsync(context);
        registered.ShouldNotBeNull("the fix provider offered no action");

        var operations = await registered.GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single()
            .ChangedSolution.GetDocument(document.Id);
        changed.ShouldNotBeNull();

        return (await changed.GetTextAsync()).ToString();
    }

    private static async Task AssertCompilesWithoutDiagnosticAsync(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "CodeFixVerification",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.ToString())
            .ShouldBeEmpty();

        var remaining = await compilation
            .WithAnalyzers([new CreateExecutionInstanceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        remaining.Where(static diagnostic => diagnostic.Id == CreateExecutionInstanceAnalyzer.DiagnosticId).ShouldBeEmpty();
    }

    private static IReadOnlyList<MetadataReference> References { get; } =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            ?? [],
        MetadataReference.CreateFromFile(typeof(Mutator<>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IMutator<>).Assembly.Location)
    ];
}
