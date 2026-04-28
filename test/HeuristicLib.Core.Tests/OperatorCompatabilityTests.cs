using System.Reflection;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace HEAL.HeuristicLib.Tests;

public class OperatorCompatabilityTests
{
  private static string GetCompilableName(Type type)
  {
    if (type.IsGenericType) {
      var genericArgs = string.Join(", ", type.GetGenericArguments().Select(t => t.FullName));
      return $"{type.Namespace}.{type.Name.Split('`')[0]}<{genericArgs}>";
    }

    return type.FullName ?? throw new InvalidOperationException("Type does not have a full name.");
  }

  private static bool AlgorithmUsingProblemDoesCompile(Type algorithmType, Type problemType)
  {
    var code = $@"
      using HEAL.HeuristicLib.Algorithms; // for RunStreamingAsync extension
      var algorithm = new {GetCompilableName(algorithmType)}();
      var problem = new {GetCompilableName(problemType)}();
      var rng = HEAL.HeuristicLib.Random.RandomNumberGenerator.Create(0);
      algorithm.RunStreamingAsync(problem, rng);
    ";

    return DoesCompile(code, typeof(object), algorithmType, problemType);
  }

  private static bool AlgorithmUsingOperatorDoesCompile(Type algorithmType, Type operatorType)
  {
    var code = $@"
      var algorithm = new {GetCompilableName(algorithmType)}();
      var @operator = new {GetCompilableName(operatorType)}();
      algorithm.Crossover = @operator;
    ";

    return DoesCompile(code, typeof(ICrossover<,,>), algorithmType, operatorType);
  }

  private static bool DoesCompile(string code, params Type[] usedTypes)
  {
    var references = usedTypes.Concat([typeof(object), typeof(IAlgorithm<,,,>), typeof(Algorithm<,,,,>)])
                              .Select(t => t.Assembly)
                              .Concat([Assembly.Load("System.Runtime")])
                              .Distinct()
                              .Select(a => MetadataReference.CreateFromFile(a.Location))
                              .ToArray();

    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var compilation = CSharpCompilation.Create(
      "TestCompilation",
      [syntaxTree],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication)
    );

    var diagnostics = compilation.GetDiagnostics();
    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    if (errors.Any()) {
      Console.WriteLine("Compilation failed with the following errors:");
      foreach (var error in errors) {
        Console.WriteLine(error.ToString());
      }
    }

    return errors.Count == 0;
  }

  [Theory]
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(TestFunctionProblem), true)]
  public void AlgorithmProblemCompatibility(Type algorithm, Type problem, bool shouldCompile) => AlgorithmUsingProblemDoesCompile(algorithm, problem).ShouldBe(shouldCompile);

  [Theory]
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(TspSpecificCrossover), false)] // incompatible problem & incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(TspSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(TspSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, PermutationProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TspSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, RealVectorProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>), typeof(TestFunctionProblemSpecificCrossover), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(TspSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(TspSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<PermutationProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TspSpecificCrossover), true)]
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<RealVectorProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TestFunctionProblemSpecificCrossover), true)]
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TspSpecificCrossover), true)]
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), true)]
  public void AlgorithmOperatorCompatibility(Type algorithm, Type @operator, bool shouldCompile) => AlgorithmUsingOperatorDoesCompile(algorithm, @operator).ShouldBe(shouldCompile);
}

public record IndependentAlgorithm<TGenotype, TSearchSpace, TProblem> : Algorithm<TGenotype, TSearchSpace, TProblem, SearchState, IndependentAlgorithm<TGenotype, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public new sealed class ExecutionState
    : Algorithm<TGenotype, TSearchSpace, TProblem, SearchState, ExecutionState>.ExecutionState
  {
  }

  public ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; set; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new() { Evaluator = resolver.Resolve(Evaluator) };

  protected override IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, SearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState) => throw new NotImplementedException();
}

public record IndependentAlgorithm<TGenotype, TSearchSpace> : IndependentAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public record IndependentAlgorithm<TGenotype> : IndependentAlgorithm<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public record PermutationEncodingSpecificAlgorithm<TProblem> : Algorithm<Permutation, PermutationSearchSpace, TProblem, SearchState, PermutationEncodingSpecificAlgorithm<TProblem>.ExecutionState>
  where TProblem : class, IProblem<Permutation, PermutationSearchSpace>
{
  public new sealed class ExecutionState
    : Algorithm<Permutation, PermutationSearchSpace, TProblem, SearchState, ExecutionState>.ExecutionState
  {
  }

  public ICrossover<Permutation, PermutationSearchSpace, TProblem> Crossover { get; set; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new() { Evaluator = resolver.Resolve(Evaluator) };

  protected override IAlgorithmInstance<Permutation, PermutationSearchSpace, TProblem, SearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState) => throw new NotImplementedException();
}

public record PermutationEncodingSpecificAlgorithm : PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>;

public record TravelingSalesmanProblemSpecificAlgorithm : Algorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SearchState, TravelingSalesmanProblemSpecificAlgorithm.ExecutionState>
{
  public new sealed class ExecutionState
    : Algorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SearchState, ExecutionState>.ExecutionState
  {
  }

  public ICrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> Crossover { get; set; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new() { Evaluator = resolver.Resolve(Evaluator) };

  protected override IAlgorithmInstance<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState) => throw new NotImplementedException();
}

public record RealVectorEncodingSpecificAlgorithm<TProblem> : Algorithm<RealVector, RealVectorSearchSpace, TProblem, SearchState, RealVectorEncodingSpecificAlgorithm<TProblem>.ExecutionState>
  where TProblem : class, IProblem<RealVector, RealVectorSearchSpace>
{
  public new sealed class ExecutionState
    : Algorithm<RealVector, RealVectorSearchSpace, TProblem, SearchState, ExecutionState>.ExecutionState
  {
  }

  public ICrossover<RealVector, RealVectorSearchSpace, TProblem> Crossover { get; set; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new() { Evaluator = resolver.Resolve(Evaluator) };

  protected override IAlgorithmInstance<RealVector, RealVectorSearchSpace, TProblem, SearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState) => throw new NotImplementedException();
}

public record RealVectorEncodingSpecificAlgorithm : RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, RealVectorSearchSpace>>;

public record TestFunctionProblemSpecificAlgorithm : Algorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SearchState, TestFunctionProblemSpecificAlgorithm.ExecutionState>
{
  public new sealed class ExecutionState
    : Algorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SearchState, ExecutionState>.ExecutionState
  {
  }

  public ICrossover<RealVector, RealVectorSearchSpace, TestFunctionProblem> Crossover { get; set; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new() { Evaluator = resolver.Resolve(Evaluator) };

  protected override IAlgorithmInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState) => throw new NotImplementedException();
}

public record IndependentCrossover<TGenotype> : SingleSolutionCrossover<TGenotype>
{
  public override TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random) => throw new NotImplementedException();
}

public record PermutationSpecificCrossover : SingleSolutionCrossover<Permutation, PermutationSearchSpace>
{
  public override Permutation Cross(IParents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace) => throw new NotImplementedException();
}

public record TspSpecificCrossover : SingleSolutionCrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
{
  public override Permutation Cross(IParents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public record RealVectorSpecificCrossover : SingleSolutionCrossover<RealVector, RealVectorSearchSpace>
{
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) => throw new NotImplementedException();
}

public record TestFunctionProblemSpecificCrossover : SingleSolutionCrossover<RealVector, RealVectorSearchSpace, TestFunctionProblem>
{
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => throw new NotImplementedException();
}
