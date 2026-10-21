using HEAL.HeuristicLib.Operators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class OperatorContractCompilationTests
{
    [Theory]
    [InlineData("IOperator<Mutation, RealVector> slot = mutation;", null)]
    [InlineData("IOperator<Mutation, string> slot = mutation;", "CS0266")]
    [InlineData("IOperator<Evaluation, RealVector> slot = mutation;", "CS0266")]
    [InlineData("IOperator<Mutation, RealVector> slot = algorithm;", "CS0266")]
    [InlineData("Forward<Mutation, RealVector>(algorithm);", "CS1503")]
    [InlineData("_ = Aspect.Create(Pointcut.OfType<IOperator<Mutation, RealVector>>(), evaluationAdvice);", "CS0311")]
    [InlineData("_ = ChoosePerCandidate.Create(mutation);", null)]
    [InlineData("_ = ChoosePerCandidate.Create<Selection, RealVector>();", "CS0311")]
    [InlineData("IOperator<Interception<PopulationState<RealVector>>, RealVector> slot = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();", null)]
    [InlineData("IOperator<Interception<PopulationState<RealVector>>, RealVector> slot = new IdentityInterceptor<RealVector, SingleSolutionState<RealVector>>();", "CS0029")]
    public void CandidateOperationAlgorithmAndAdviceBoundariesAreCheckedByTheCompiler(string statement, string? expectedDiagnostic)
    {
        var tree = CSharpSyntaxTree.ParseText($$"""
            using HEAL.HeuristicLib.Algorithms;
            using HEAL.HeuristicLib.Encodings.RealVectors;
            using HEAL.HeuristicLib.Execution;
            using HEAL.HeuristicLib.Operators;

            public static class Consumer
            {
                public static void Use(IOperator<Mutation, RealVector> mutation, IAlgorithm<RealVector> algorithm,
                    OperatorAdvice<Evaluation, RealVector> evaluationAdvice)
                {
                    {{statement}}
                }

                private static void Forward<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
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
