using HEAL.HeuristicLib.Operators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests.Operators;

public class TypeCompatibilityTests
{
    [Fact]
    public void InvalidCodeCompilation_ReturnsErrorDiagnostic()
    {
        var hlAssembly = typeof(ICreator<,,>).Assembly;
        var compilation = CSharpCompilation.Create("asd")
          .AddReferences(MetadataReference.CreateFromFile(hlAssembly.Location))
          .AddSyntaxTrees(CSharpSyntaxTree.ParseText(@"
        using HEAL.HeuristicLib.Operators;

        public class
    ", cancellationToken: TestContext.Current.CancellationToken));

        var diagnostics = compilation.GetDiagnostics(cancellationToken: TestContext.Current.CancellationToken);

        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error);
    }
}
