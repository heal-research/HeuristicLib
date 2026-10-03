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

/// <summary>Apply the offered fix and compile it against the real library, including custom execution contracts.</summary>
public class ExecutionFactoryCodeFixTests
{
    private const string Preamble = """
        using System.Collections.Generic;
        using HEAL.HeuristicLib.Algorithms;
        using HEAL.HeuristicLib.Execution;
        using HEAL.HeuristicLib.Operators;
        using HEAL.HeuristicLib.Operators.Mutators;
        using HEAL.HeuristicLib.Problems;
        using HEAL.HeuristicLib.Random;
        using HEAL.HeuristicLib.SearchSpaces;
        using SS = HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>;
        using P = HEAL.HeuristicLib.Problems.IProblem<int, HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>>;

        file sealed record ChildMutator : Mutator<int>
        {
            public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => _ => new Execution();
            private sealed class Execution : MutatorExecution<int, SS, P>
            {
                public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
            }
        }

        """;

    [Theory]
    [InlineData("IMutator<int>", "Child.CreateExecutionFactory<SS, P>()(scope)")]
    [InlineData("ChildMutator", "Child.CreateExecutionFactory()(scope)")]
    [InlineData("ChildMutator", "(Child.CreateExecutionFactory())(scope)")]
    [InlineData("ChildMutator", "Child.CreateExecutionFactory().Invoke(scope)")]
    public async Task FixedCode_Compiles_ForImmediateFactoryInvocation(string childType, string binding)
    {
        var fixedSource = await ApplyFixAsync(Preamble + $$"""
            file sealed record BypassingMutator : Mutator<int>
            {
                public required {{childType}} Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => scope => {{binding}};
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_WhenPreparationReturnsTheChildFactory()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record BypassingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => Child.CreateExecutionFactory<SS, P>();
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForDeferredResolutionThroughAScopeField()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed class DeferredExecution(IMutator<int> child, ResolutionScope scope) : MutatorExecution<int, SS, P>
            {
                public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) =>
                    Activate().Mutate(parents, random, searchSpace, problem);
                private IMutatorExecution<int, SS, P> Activate() => child.CreateExecutionFactory<SS, P>()(scope.CreateChildScope());
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.CreateChildScope().Resolve<int,SS,P>(child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAFourArgumentRole()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record BypassingInterceptor : IConfigurationNode<IInterceptorExecution<int, SS, P, PopulationState<int>>>
            {
                public required IInterceptor<int> Child { get; init; }
                public ExecutionFactory<IInterceptorExecution<int, SS, P, PopulationState<int>>> CreateExecutionFactory() =>
                    scope => Child.CreateExecutionFactory<SS, P, PopulationState<int>>()(scope);
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P,PopulationState<int>>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedExecutionContract()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file interface ICustomExecution : IExecutionNode;
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecution>
            {
                public required IConfigurationNode<ICustomExecution> Child { get; init; }
                public ExecutionFactory<ICustomExecution> CreateExecutionFactory() => scope => Child.CreateExecutionFactory()(scope);
            }
            """);

        fixedSource.ShouldContain("scope.Resolve(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedRoleAndResolver()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file interface ICustomRole<T> : IConfigurationNode
            {
                ExecutionFactory<CustomExecution<T>> CreateExecutionFactory();
            }
            file sealed class CustomExecution<T> : IExecutionNode;
            file sealed record CustomConfiguration : ICustomRole<int>
            {
                public required ICustomRole<int> Child { get; init; }
                public ExecutionFactory<CustomExecution<int>> CreateExecutionFactory() => scope => Child.CreateExecutionFactory()(scope);
            }
            file static class CustomResolution
            {
                public static CustomExecution<T> Resolve<T>(this ResolutionScope scope, ICustomRole<T> source) =>
                    scope.Resolve(source, child => child.CreateExecutionFactory());
            }
            """);

        fixedSource.ShouldContain("scope.Resolve<int>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task DoesNotOfferAnUncompilableFix_WhenACustomRoleHasNoResolver()
    {
        await ApplyFixAsync(Preamble + """
            file interface ICustomRole : IConfigurationNode
            {
                ExecutionFactory<ICustomExecution> CreateExecutionFactory();
            }
            file interface ICustomExecution : IExecutionNode;
            file sealed record CustomConfiguration : ICustomRole
            {
                public required ICustomRole Child { get; init; }
                public ExecutionFactory<ICustomExecution> CreateExecutionFactory() => scope => Child.CreateExecutionFactory()(scope);
            }
            """, expectFix: false);
    }

    [Fact]
    public async Task DoesNotOfferAPartialFix_ForAStoredFactory()
    {
        await ApplyFixAsync(Preamble + """
            file sealed record BypassingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory()
                {
                    var factory = Child.CreateExecutionFactory<SS, P>();
                    return scope => factory(scope);
                }
            }
            """, expectFix: false);
    }

    private static async Task<string> ApplyFixAsync(string source, bool expectFix = true)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("CodeFixTest", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))
            .AddMetadataReferences(References);
        var document = project.AddDocument("Input.cs", SourceText.From(source));
        var compilation = await document.Project.GetCompilationAsync();
        compilation.ShouldNotBeNull();
        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await compilation.WithAnalyzers([new ExecutionFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        var diagnostic = diagnostics.Single(d => d.Id == ExecutionFactoryAnalyzer.DiagnosticId);

        CodeAction? registered = null;
        var context = new CodeFixContext(document, diagnostic, (action, _) => registered = action, CancellationToken.None);
        await new ExecutionFactoryCodeFixProvider().RegisterCodeFixesAsync(context);
        if (!expectFix)
        {
            registered.ShouldBeNull();
            return source;
        }
        registered.ShouldNotBeNull("the fix provider offered no action");
        var operations = await registered.GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id);
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
        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var remaining = await compilation.WithAnalyzers([new ExecutionFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        remaining.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        remaining.Where(static diagnostic => diagnostic.Id == ExecutionFactoryAnalyzer.DiagnosticId).ShouldBeEmpty();
    }

    private static IReadOnlyList<MetadataReference> References { get; } =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path)) ?? [],
        MetadataReference.CreateFromFile(typeof(Mutator<>).Assembly.Location)
    ];
}
