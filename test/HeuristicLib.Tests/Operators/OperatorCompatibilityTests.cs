using System.Reflection;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests.Operators;

public class OperatorCompatibilityTests
{
    private static string GetCompilableName(Type type)
    {
        if (type.IsGenericType)
        {
            // Recursive, so a generic type argument such as IProblem<Permutation, PermutationSearchSpace> renders as
            // C# source rather than as its backtick metadata name.
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetCompilableName));
            return $"{type.Namespace}.{type.Name.Split('`')[0]}<{genericArgs}>";
        }

        return type.FullName ?? throw new InvalidOperationException("Type does not have a full name.");
    }

    private static bool AlgorithmUsingProblemDoesCompile(Type algorithmType, Type problemType)
    {
        var code = $@"
      using HEAL.HeuristicLib.Algorithms;
      var algorithm = new {GetCompilableName(algorithmType)}();
      var problem = new {GetCompilableName(problemType)}();
      var rng = HEAL.HeuristicLib.Random.RandomNumberGenerator.Create(0);
      algorithm.Stream(problem, rng);
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

        return DoesCompile(code, typeof(ICrossover<>), algorithmType, operatorType);
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

        if (errors.Any())
        {
            Console.WriteLine("Compilation failed with the following errors:");
            foreach (var error in errors)
            {
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
    [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(TravelingSalesmanProblem), true)]
    [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TravelingSalesmanProblem), true)]
    [InlineData(typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(TestFunctionProblem), true)]
    [InlineData(typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TestFunctionProblem), true)]
    [InlineData(typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(TestFunctionProblem), true)]
    [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(TravelingSalesmanProblem), true)]
    [InlineData(typeof(PermutationEncodingSpecificAlgorithm), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(TravelingSalesmanProblem), true)]
    [InlineData(typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TravelingSalesmanProblem), true)]
    [InlineData(typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(RealVectorEncodingSpecificAlgorithm), typeof(TestFunctionProblem), true)]
    [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TestFunctionProblem), true)]
    [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TestFunctionProblem), true)]
    [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TravelingSalesmanProblem), true)]
    [InlineData(typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TestFunctionProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(TravelingSalesmanProblem), false)] // incompatible problem (wrong encoding)
    [InlineData(typeof(TestFunctionProblemSpecificAlgorithm), typeof(TestFunctionProblem), true)]
    public void AlgorithmProblemCompatibility(Type algorithm, Type problem, bool shouldCompile) => AlgorithmUsingProblemDoesCompile(algorithm, problem).ShouldBe(shouldCompile);

    /// <summary>
    /// Every algorithm and crossover pairing, with what the compiler accepts and what trial resolution accepts.
    /// </summary>
    /// <remarks>
    /// The two columns are the measurement. Before the crossover role dropped its search space and problem they
    /// were identical: assigning an operator into an algorithm slot was the compatibility check. The role now names
    /// only its candidate, so the compiler checks that alone and the search space and problem are checked when the
    /// execution instance is built. Every row where the columns differ is a check that moved from build time to
    /// pre-flight, and the validation column still holds the answers the compiler used to give.
    /// </remarks>
    public static TheoryData<Type, Type, bool, bool> CrossoverCompatibility => new()
    {
        { typeof(IndependentAlgorithm<Permutation>), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(IndependentAlgorithm<Permutation>), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation>), typeof(PermutationSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<Permutation>), typeof(TspSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<Permutation>), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation>), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<RealVector>), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector>), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(IndependentAlgorithm<RealVector>), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector>), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<RealVector>), typeof(RealVectorSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<RealVector>), typeof(TestFunctionProblemSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(PermutationSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(TspSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace>), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(PermutationSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(TspSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(PermutationSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TspSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace>), typeof(TestFunctionProblemSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TestFunctionProblemSpecificCrossover), true, false },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(IndependentAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>), typeof(TestFunctionProblemSpecificCrossover), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(PermutationEncodingSpecificAlgorithm), typeof(PermutationSpecificCrossover), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm), typeof(TspSpecificCrossover), true, false },
        { typeof(PermutationEncodingSpecificAlgorithm), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(PermutationEncodingSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(PermutationSpecificCrossover), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(TspSpecificCrossover), true, false },
        { typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(PermutationSpecificCrossover), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TspSpecificCrossover), true, true },
        { typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(PermutationEncodingSpecificAlgorithm<TravelingSalesmanProblem>), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(RealVectorEncodingSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(RealVectorEncodingSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(RealVectorEncodingSpecificAlgorithm), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(RealVectorEncodingSpecificAlgorithm), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(RealVectorEncodingSpecificAlgorithm), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(RealVectorEncodingSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), true, false },
        { typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>), typeof(TestFunctionProblemSpecificCrossover), true, false },
        { typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(RealVectorEncodingSpecificAlgorithm<TestFunctionProblem>), typeof(TestFunctionProblemSpecificCrossover), true, true },
        { typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), true, true },
        { typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), false, false }, // incompatible encoding
        { typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(PermutationSpecificCrossover), true, true },
        { typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TspSpecificCrossover), true, true },
        { typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(RealVectorSpecificCrossover), false, false }, // incompatible encoding
        { typeof(TravelingSalesmanProblemSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(TestFunctionProblemSpecificAlgorithm), typeof(IndependentCrossover<Permutation>), false, false }, // incompatible encoding
        { typeof(TestFunctionProblemSpecificAlgorithm), typeof(IndependentCrossover<RealVector>), true, true },
        { typeof(TestFunctionProblemSpecificAlgorithm), typeof(PermutationSpecificCrossover), false, false }, // incompatible encoding
        { typeof(TestFunctionProblemSpecificAlgorithm), typeof(TspSpecificCrossover), false, false }, // incompatible encoding and incompatible problem
        { typeof(TestFunctionProblemSpecificAlgorithm), typeof(RealVectorSpecificCrossover), true, true },
        { typeof(TestFunctionProblemSpecificAlgorithm), typeof(TestFunctionProblemSpecificCrossover), true, true },
    };

    /// <summary>
    /// What the compiler accepts. Since the crossover role names only its candidate, this now measures the candidate
    /// alone: a crossover written for a narrower search space or problem assigns into any algorithm over the same
    /// candidate, and is rejected only when the candidate itself differs.
    /// </summary>
    [Theory]
    [MemberData(nameof(CrossoverCompatibility))]
    public void AlgorithmOperatorCompatibility(Type algorithm, Type @operator, bool shouldCompile, bool shouldValidate)
    {
        _ = shouldValidate;
        AlgorithmUsingOperatorDoesCompile(algorithm, @operator).ShouldBe(shouldCompile);
    }

    /// <summary>
    /// What trial resolution accepts, which is where the search space and problem are now checked. These are the
    /// answers the compiler gave before the role migrated, so nothing stopped being checked — the check moved.
    /// </summary>
    /// <remarks>
    /// The check is <c>registry.TryResolve(...)</c>, which reports rather than throws and hands back the instance the
    /// run will use when it succeeds — so a pairing that validates here cannot fail when the run builds it.
    /// <see cref="AlgorithmValidationExtensions"/> answers a different question, about declared search invariants.
    /// </remarks>
    [Theory]
    [MemberData(nameof(CrossoverCompatibility))]
    public void AlgorithmOperatorValidation(Type algorithm, Type @operator, bool shouldCompile, bool shouldValidate)
    {
        _ = shouldCompile;
        OperatorValidatesForAlgorithm(algorithm, @operator).ShouldBe(shouldValidate);
    }

    /// <summary>Reads the run's triple off the algorithm's own declaration.</summary>
    private static (Type Candidate, Type SearchSpace, Type Problem) TripleOf(Type algorithmType)
    {
        for (var type = algorithmType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Algorithm<,,,,>))
            {
                var arguments = type.GetGenericArguments();
                return (arguments[1], arguments[2], arguments[3]);
            }
        }

        throw new InvalidOperationException($"{algorithmType.Name} does not derive from Algorithm<,,,,>.");
    }

    /// <summary>
    /// Asks the binding check the question the compiler used to answer: can this operator serve a run over the
    /// algorithm's search space and problem?
    /// </summary>
    private static bool OperatorValidatesForAlgorithm(Type algorithmType, Type operatorType)
    {
        var (candidate, searchSpace, problem) = TripleOf(algorithmType);
        var @operator = Activator.CreateInstance(operatorType)!;

        // A crossover for a different candidate is not the same role at all, so it never reaches the binding check.
        if (!typeof(ICrossover<>).MakeGenericType(candidate).IsInstanceOfType(@operator))
        {
            return false;
        }

        var tryResolve = typeof(CrossoverResolverExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(CrossoverResolverExtensions.TryResolve)
                              && method.GetParameters() is [{ ParameterType.Name: nameof(ExecutionInstanceRegistry) }, _, _, _])
            .MakeGenericMethod(candidate, searchSpace, problem);

        var arguments = new object?[] { new ExecutionInstanceRegistry(), @operator, null, null };
        var validates = (bool)tryResolve.Invoke(null, arguments)!;

        // The contract the check carries: a true result hands back the instance the run will use, and a false result
        // says why. Asserting both here is what makes "validated" mean "creation will work".
        if (validates)
        {
            arguments[2].ShouldNotBeNull();
            arguments[3].ShouldBeNull();
        }
        else
        {
            arguments[2].ShouldBeNull();
            arguments[3].ShouldNotBeNull();
        }

        return validates;
    }

}

public record IndependentAlgorithm<TCandidate, TSearchSpace, TProblem> : Algorithm<IndependentAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, SearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICrossover<TCandidate> Crossover { get; set; } = new IndependentCrossover<TCandidate>();

    public override AlgorithmInstance<TCandidate, TSearchSpace, TProblem, SearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => throw new NotSupportedException();
}

public record IndependentAlgorithm<TCandidate, TSearchSpace> : IndependentAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public record IndependentAlgorithm<TCandidate> : IndependentAlgorithm<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public record PermutationEncodingSpecificAlgorithm<TProblem> : Algorithm<PermutationEncodingSpecificAlgorithm<TProblem>, Permutation, PermutationSearchSpace, TProblem, SearchState>
    where TProblem : class, IProblem<Permutation, PermutationSearchSpace>
{
    public ICrossover<Permutation> Crossover { get; set; } = new PermutationSpecificCrossover();

    public override AlgorithmInstance<Permutation, PermutationSearchSpace, TProblem, SearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => throw new NotSupportedException();
}

public record PermutationEncodingSpecificAlgorithm : PermutationEncodingSpecificAlgorithm<IProblem<Permutation, PermutationSearchSpace>>;

public record TravelingSalesmanProblemSpecificAlgorithm : Algorithm<TravelingSalesmanProblemSpecificAlgorithm, Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SearchState>
{
    public ICrossover<Permutation> Crossover { get; set; } = new TspSpecificCrossover();

    public override AlgorithmInstance<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => throw new NotSupportedException();
}

public record RealVectorEncodingSpecificAlgorithm<TProblem> : Algorithm<RealVectorEncodingSpecificAlgorithm<TProblem>, RealVector, BoundedRealVectorSearchSpace, TProblem, SearchState>
    where TProblem : class, IProblem<RealVector, BoundedRealVectorSearchSpace>
{
    public ICrossover<RealVector> Crossover { get; set; } = new RealVectorSpecificCrossover();

    public override AlgorithmInstance<RealVector, BoundedRealVectorSearchSpace, TProblem, SearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => throw new NotSupportedException();
}

public record RealVectorEncodingSpecificAlgorithm : RealVectorEncodingSpecificAlgorithm<IProblem<RealVector, BoundedRealVectorSearchSpace>>;

public record TestFunctionProblemSpecificAlgorithm : Algorithm<TestFunctionProblemSpecificAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SearchState>
{
    public ICrossover<RealVector> Crossover { get; set; } = new TestFunctionProblemSpecificCrossover();

    public override AlgorithmInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => throw new NotSupportedException();
}

public record IndependentCrossover<TCandidate> : SingleCandidateCrossover<TCandidate>
{
    public override TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random) => throw new NotSupportedException();
}

public record PermutationSpecificCrossover : SingleCandidateCrossover<Permutation, PermutationSearchSpace>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace) => throw new NotSupportedException();
}

public record TspSpecificCrossover : SingleCandidateCrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem) => throw new NotSupportedException();
}

public record RealVectorSpecificCrossover : SingleCandidateCrossover<RealVector, BoundedRealVectorSearchSpace>
{
    public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) => throw new NotSupportedException();
}

public record TestFunctionProblemSpecificCrossover : SingleCandidateCrossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => throw new NotSupportedException();
}
