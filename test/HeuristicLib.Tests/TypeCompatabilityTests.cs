using HEAL.HeuristicLib.Operators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests;

public class TypeCompatabilityTests {
  [Fact]
  public void TestCompatabilityTest1() {
    var hlAssembly = typeof(ICreator<,,>).Assembly;
    var compilation = CSharpCompilation.Create("asd")
                                       .AddReferences(MetadataReference.CreateFromFile(hlAssembly.Location))
                                       .AddSyntaxTrees(CSharpSyntaxTree.ParseText(@"
        using HEAL.HeuristicLib.Operators;

        public class
    "));

    var diagnostics = compilation.GetDiagnostics();

    diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error);
  }
}
