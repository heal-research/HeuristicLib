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
    [InlineData("_ = ChoosePerItem.Create(mutation);", null)]
    [InlineData("_ = ChoosePerItem.Create<Selection, RealVector>();", "CS0311")]
    [InlineData("IOperator<Interception<PopulationState<RealVector>>, RealVector> slot = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();", null)]
    [InlineData("IOperator<Interception<PopulationState<RealVector>>, RealVector> slot = new IdentityInterceptor<RealVector, SingleSolutionState<RealVector>>();", "CS0029")]
    public void CandidateOperationAlgorithmAndAdviceBoundariesAreCheckedByTheCompiler(string statement, string? expectedDiagnostic)
    {
        var errors = Errors($$"""
            public static class Consumer
            {
                public static void Use(IOperator<Mutation, RealVector> mutation, IAlgorithm<RealVector> algorithm,
                    OperatorAdvice<Evaluation, RealVector> evaluationAdvice)
                {
                    {{statement}}
                }

                private static void Forward<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
                    where TOperation : IOperation<TOperation> { }
            }
            """);
        if (expectedDiagnostic is null)
            errors.ShouldBeEmpty();
        else
        {
            errors.Length.ShouldBe(1);
            errors[0].Id.ShouldBe(expectedDiagnostic);
        }
    }

    // The hook names no operation, so a configuration performing several operations can only have one.
    [Theory]
    [InlineData(OneHook, null)]
    [InlineData(ExplicitHookPerOperation, "CS0539")]
    [InlineData(DefaultHookPerOperation, "CS8705")]
    public void TheCompilerAllowsOneConstructionHookPerCandidate(string declarations, string? expectedDiagnostic)
    {
        var errors = Errors(declarations);

        if (expectedDiagnostic is null)
            errors.ShouldBeEmpty();
        else
            errors.ShouldContain(error => error.Id == expectedDiagnostic);
    }

    private const string OneHook = """
        public sealed class Configuration : IOperator<Mutation, RealVector>, IOperator<Evaluation, RealVector>
        {
            public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
                where TSearchSpace : class, ISearchSpace<RealVector>
                where TProblem : class, IProblem<RealVector, TSearchSpace>
                where TSearchState : class, ISearchState => throw new NotSupportedException();
        }
        """;

    private const string ExplicitHookPerOperation = """
        public sealed class Configuration : IOperator<Mutation, RealVector>, IOperator<Evaluation, RealVector>
        {
            IExecutor IOperator<Mutation, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => throw new NotSupportedException();

            IExecutor IOperator<Evaluation, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => throw new NotSupportedException();
        }
        """;

    private const string DefaultHookPerOperation = """
        public interface IDefaultHook<TOperation> : IOperator<TOperation, RealVector>
            where TOperation : IOperation<TOperation>
        {
            IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => throw new NotSupportedException();
        }

        public sealed class Configuration : IDefaultHook<Mutation>, IDefaultHook<Evaluation>;
        """;

    private static Diagnostic[] Errors(string declarations)
    {
        var tree = CSharpSyntaxTree.ParseText($$"""
            using System;
            using HEAL.HeuristicLib.Algorithms;
            using HEAL.HeuristicLib.Encodings.RealVectors;
            using HEAL.HeuristicLib.Execution;
            using HEAL.HeuristicLib.Operators;
            using HEAL.HeuristicLib.Problems;
            using HEAL.HeuristicLib.SearchSpaces;

            {{declarations}}
            """);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(IOperator).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("OperatorContractConsumer", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    }
}
