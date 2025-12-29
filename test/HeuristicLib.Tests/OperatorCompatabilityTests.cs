using System.Reflection;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
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

public class OperatorCompatabilityTests {
  private static string GetCompilableName(Type type) {
    if (type.IsGenericType) {
      string genericArgs = string.Join(", ", type.GetGenericArguments().Select(t => t.FullName));
      return $"{type.Namespace}.{type.Name.Split('`')[0]}<{genericArgs}>";
    }

    return type.FullName ?? throw new InvalidOperationException("Type does not have a full name.");
  }

  private static bool AlgorithmUsingProblemDoesCompile(Type algorithmType, Type problemType) {
    string code = $@"
      var algorithm = new {GetCompilableName(algorithmType)}();
      var problem = new {GetCompilableName(problemType)}();
      algorithm.Execute(problem);
    ";

    return DoesCompile(code, typeof(object), algorithmType, problemType);
  }

  private static bool AlgorithmUsingOperatorDoesCompile(Type algorithmType, Type operatorType) {
    string code = $@"
      var algorithm = new {GetCompilableName(algorithmType)}();
      var @operator = new {GetCompilableName(operatorType)}();
      algorithm.Crossover = @operator;
    ";

    return DoesCompile(code, typeof(ICrossover<,,>), algorithmType, operatorType);
  }

  private static bool DoesCompile(string code, params Type[] usedTypes) {
    var references = usedTypes.Append([typeof(object)])
                              .Select(t => t.Assembly)
                              .Append([Assembly.Load("System.Runtime")])
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
    var containsError = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    return !containsError;
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
  public void AlgorithmProblemCompatibility(Type algorithm, Type problem, bool shouldCompile) {
    AlgorithmUsingProblemDoesCompile(algorithm, problem).ShouldBe(shouldCompile);
  }

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
  public void AlgorithmOperatorCompatibility(Type algorithm, Type @operator, bool shouldCompile) {
    AlgorithmUsingOperatorDoesCompile(algorithm, @operator).ShouldBe(shouldCompile);
  }
}

public abstract record AlgorithmState<TGenotype> : IterationState;

public class IndependentAlgorithm<TGenotype, TSearchSpace, TProblem> : Algorithm<TGenotype, TSearchSpace, TProblem, AlgorithmState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; set; }

  public override AlgorithmState<TGenotype> Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class IndependentAlgorithm<TGenotype, TSearchSpace> : IndependentAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype> { }

public class IndependentAlgorithm<TGenotype> : IndependentAlgorithm<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> { }

public class PermutationEncodingSpecificAlgorithm<TProblem> : Algorithm<Permutation, PermutationSearchSpace, TProblem, AlgorithmState<Permutation>>
  where TProblem : class, IProblem<Permutation, PermutationSearchSpace> {
  public ICrossover<Permutation, PermutationSearchSpace, TProblem> Crossover { get; set; }

  public override AlgorithmState<Permutation> Execute(TProblem problem, PermutationSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class PermutationEncodingSpecificAlgorithm : PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>> { }

public class TravelingSalesmanProblemSpecificAlgorithm : Algorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, AlgorithmState<Permutation>> {
  public ICrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> Crossover { get; set; }

  public override AlgorithmState<Permutation> Execute(TravelingSalesmanProblem problem, PermutationSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithm<TProblem> : Algorithm<RealVector, RealVectorSearchSpace, TProblem, AlgorithmState<RealVector>>
  where TProblem : class, IProblem<RealVector, RealVectorSearchSpace> {
  public ICrossover<RealVector, RealVectorSearchSpace, TProblem> Crossover { get; set; }

  public override AlgorithmState<RealVector> Execute(TProblem problem, RealVectorSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithm : RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, RealVectorSearchSpace>> { }

public class TestFunctionProblemSpecificAlgorithm : Algorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, AlgorithmState<RealVector>> {
  public ICrossover<RealVector, RealVectorSearchSpace, TestFunctionProblem> Crossover { get; set; }

  public override AlgorithmState<RealVector> Execute(TestFunctionProblem problem, RealVectorSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class IndependentCrossover<TGenotype> : Crossover<TGenotype> {
  public override TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover : Crossover<Permutation, PermutationSearchSpace> {
  public override Permutation Cross(IParents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace) => throw new NotImplementedException();
}

public class TspSpecificCrossover : Crossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
  public override Permutation Cross(IParents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover : Crossover<RealVector, RealVectorSearchSpace> {
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover : Crossover<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => throw new NotImplementedException();
}
