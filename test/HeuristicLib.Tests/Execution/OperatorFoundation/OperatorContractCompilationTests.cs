using HEAL.HeuristicLib.Operators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class OperatorContractCompilationTests
{
    [Theory]
    [InlineData("IOperator<RealVector, Mutation> slot = mutation;", null)]
    [InlineData("IOperator<string, Mutation> slot = mutation;", "CS0266")]
    [InlineData("IOperator<RealVector, Evaluation> slot = mutation;", "CS0266")]
    [InlineData("IOperator<RealVector, Mutation> slot = algorithm;", "CS0266")]
    [InlineData("Forward<RealVector, Mutation>(algorithm);", "CS1503")]
    [InlineData("_ = Aspect.Create(Pointcut.OfType<IOperator<RealVector, Mutation>>(), evaluationAdvice);", "CS0311")]
    public void CandidateOperationAlgorithmAndAdviceBoundariesAreCheckedByTheCompiler(string statement, string? expectedDiagnostic)
    {
        var tree = CSharpSyntaxTree.ParseText($$"""
            using HEAL.HeuristicLib.Algorithms;
            using HEAL.HeuristicLib.Encodings.RealVectors;
            using HEAL.HeuristicLib.Execution;
            using HEAL.HeuristicLib.Operators;

            public static class Consumer
            {
                public static void Use(IOperator<RealVector, Mutation> mutation, IAlgorithm<RealVector> algorithm,
                    OperatorAdvice<RealVector, Evaluation> evaluationAdvice)
                {
                    {{statement}}
                }

                private static void Forward<TCandidate, TOperation>(IOperator<TCandidate, TOperation> source)
                    where TOperation : IOperationDescriptor<TOperation> { }
            }
            """);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(IOperator).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("OperatorContractConsumer", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (expectedDiagnostic is null)
            errors.ShouldBeEmpty();
        else
        {
            errors.Length.ShouldBe(1);
            errors[0].Id.ShouldBe(expectedDiagnostic);
        }
    }
}
