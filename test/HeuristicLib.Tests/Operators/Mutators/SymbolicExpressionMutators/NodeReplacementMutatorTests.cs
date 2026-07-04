using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Operators.SymbolicExpressions;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.SymbolicExpressionMutators;

public sealed class NodeReplacementMutatorTests
{
    [Fact]
    public void Mutate_ReplacesOperationWithSameArityOperationFromSearchSpace()
    {
        var parent = (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();
        var searchSpace = CreateSearchSpace(["x0", "x1"]);

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.9, 0.3),
          searchSpace);

        parent.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        mutant.ToInfixString().ShouldBe("(x0 - (2 * x1))");
        mutant.Evaluate(["x0", "x1"], [1.0, 3.0]).ShouldBe(-5.0);
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesVariableWithAllowedVariable()
    {
        var parent = Variable("x0").Compile();
        var searchSpace = CreateSearchSpace(["x0", "x1"]);

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.0, 0.0, 0.9),
          searchSpace);

        mutant.ToInfixString().ShouldBe("x1");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_UsesInstanceEntryPointWithSearchSpace()
    {
        var parent = Sqrt(Variable("x0")).Compile();
        var searchSpace = CreateSearchSpace(["x0"]);

        var mutant = new NodeReplacementMutator()
          .Mutate(parent, new SequenceRandomNumberGenerator(0.9, 0.0), searchSpace);

        mutant.ToInfixString().ShouldBe("log(x0)");
        mutant.Evaluate(["x0"], [Math.E]).ShouldBe(1.0, tolerance: 1e-12);
    }

    [Fact]
    public void Mutate_OnlyUsesOperationsAllowedBySearchSpace()
    {
        var parent = (Fixed(1.0) * Fixed(2.0)).Compile();
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 10,
            maximumDepth: 5,
            allowedOperations:
            [
                SymbolicExpressionOpCode.Add,
                SymbolicExpressionOpCode.Multiply
            ],
            allowedVariables: []);

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.9, 0.0),
          searchSpace);

        mutant.ToInfixString().ShouldBe("(1 + 2)");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_AllowsNullMutation()
    {
        var parent = Variable("x0").Compile();
        var searchSpace = CreateSearchSpace(["x0", "x1"]);

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.0, 0.0, 0.0),
          searchSpace);

        mutant.ToInfixString().ShouldBe("x0");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesNumericLiteralWithAllowedVariable()
    {
        var parent = Fixed(100.0).Compile();
        var searchSpace = CreateSearchSpace(["x0"]);

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.0, 0.0, 0.0),
          searchSpace);

        mutant.ToInfixString().ShouldBe("x0");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesVariableWithNumericLiteral()
    {
        var parent = Variable("x0").Compile();
        var searchSpace = CreateSearchSpace(["x0"]);
        var samplingProfile = new SymbolicExpressionSamplingProfile
        {
            NumericLiteralInitializationDistribution = new UniformDoubleDistribution(10.0, 14.0)
        };

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.0, 0.9, 0.75),
          searchSpace,
          samplingProfile);

        mutant.ToInfixString().ShouldBe("13");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_UsesVariableTerminalWhenOnlyVariablesAreAvailable()
    {
        var parent = Variable("x0").Compile();
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedOperations: [],
            allowedVariables: ["x0", "x1"],
            allowNumericLiterals: false);

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.0, 0.0, 0.9),
          searchSpace);

        mutant.ToInfixString().ShouldBe("x1");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_UsesNumericLiteralTerminalWhenOnlyNumericLiteralsAreAvailable()
    {
        var parent = Fixed(1.0).Compile();
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedOperations: [],
            allowedVariables: []);
        var samplingProfile = new SymbolicExpressionSamplingProfile
        {
            NumericLiteralInitializationDistribution = new UniformDoubleDistribution(10.0, 14.0)
        };

        var mutant = NodeReplacementMutation.Mutate(
          parent,
          new SequenceRandomNumberGenerator(0.0, 0.0, 0.5),
          searchSpace,
          samplingProfile);

        mutant.ToInfixString().ShouldBe("12");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    private static SymbolicExpressionSearchSpace CreateSearchSpace(IReadOnlyList<string> variables)
    {
        return new SymbolicExpressionSearchSpace(
            maximumLength: 20,
            maximumDepth: 10,
            allowedOperations: SymbolicExpressionOpCodes.BasicArithmetic,
            allowedVariables: variables);
    }
}
