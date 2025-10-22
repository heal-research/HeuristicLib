using System.Reflection;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
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
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(TravelingSalesmanProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(TestFunctionProblem), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(TestFunctionProblem), true)]
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
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(TspSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(TspSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, PermutationProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(IndependentCrossover<Permutation>), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(IndependentCrossover<RealVector>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(PermutationSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(TspSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(RealVectorSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, RealVectorProblem>), typeof(TestFunctionProblemSpecificCrossover), false)] // incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(IndependentCrossover<Permutation>), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(IndependentCrossover<RealVector>), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(PermutationSpecificCrossover), false)] // incompatible encoding
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(TspSpecificCrossover), false)] // incompatible encoding and incompatible problem
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(RealVectorSpecificCrossover), true)]
  [InlineData(typeof(IndependentAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>), typeof(TestFunctionProblemSpecificCrossover), true)]
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

public abstract record AlgorithmResult<TGenotype> : IAlgorithmResult;

public class IndependentAlgorithm<TGenotype, TEncoding, TProblem> : Algorithm<TGenotype, TEncoding, TProblem, AlgorithmResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }

  public override AlgorithmResult<TGenotype> Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class IndependentAlgorithm<TGenotype, TEncoding> : IndependentAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> { }

public class IndependentAlgorithm<TGenotype> : IndependentAlgorithm<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> { }

public class PermutationEncodingSpecificAlgorithm<TProblem> : Algorithm<Permutation, PermutationEncoding, TProblem, AlgorithmResult<Permutation>>
  where TProblem : class, IProblem<Permutation, PermutationEncoding> {
  public ICrossover<Permutation, PermutationEncoding, TProblem> Crossover { get; set; }

  public override AlgorithmResult<Permutation> Execute(TProblem problem, PermutationEncoding? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class PermutationEncodingSpecificAlgorithm : PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationEncoding>> { }

public class TravelingSalesmanProblemSpecificAlgorithm : Algorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem, AlgorithmResult<Permutation>> {
  public ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }

  public override AlgorithmResult<Permutation> Execute(TravelingSalesmanProblem problem, PermutationEncoding? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithm<TProblem> : Algorithm<RealVector, RealVectorEncoding, TProblem, AlgorithmResult<RealVector>>
  where TProblem : class, IProblem<RealVector, RealVectorEncoding> {
  public ICrossover<RealVector, RealVectorEncoding, TProblem> Crossover { get; set; }

  public override AlgorithmResult<RealVector> Execute(TProblem problem, RealVectorEncoding? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithm : RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, RealVectorEncoding>> { }

public class TestFunctionProblemSpecificAlgorithm : Algorithm<RealVector, RealVectorEncoding, TestFunctionProblem, AlgorithmResult<RealVector>> {
  public ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }

  public override AlgorithmResult<RealVector> Execute(TestFunctionProblem problem, RealVectorEncoding? searchSpace = null, IRandomNumberGenerator? random = null) => throw new NotImplementedException();
}

public class IndependentCrossover<TGenotype> : Crossover<TGenotype> {
  public override TGenotype Cross((TGenotype, TGenotype) parents, IRandomNumberGenerator random) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover : Crossover<Permutation, PermutationEncoding> {
  public override Permutation Cross((Permutation, Permutation) parents, IRandomNumberGenerator random, PermutationEncoding encoding) => throw new NotImplementedException();
}

public class TspSpecificCrossover : Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> {
  public override Permutation Cross((Permutation, Permutation) parents, IRandomNumberGenerator random, PermutationEncoding encoding, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover : Crossover<RealVector, RealVectorEncoding> {
  public override RealVector Cross((RealVector, RealVector) parents, IRandomNumberGenerator random, RealVectorEncoding encoding) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover : Crossover<RealVector, RealVectorEncoding, TestFunctionProblem> {
  public override RealVector Cross((RealVector, RealVector) parents, IRandomNumberGenerator random, RealVectorEncoding encoding, TestFunctionProblem problem) => throw new NotImplementedException();
}
