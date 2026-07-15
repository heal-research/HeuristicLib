using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.SymbolicExpressionMutators;

public sealed class NodeReplacementMutatorTests
{
    [Fact]
    public void Mutate_ReplacesWithCompatibleSymbolAndUsesItsInitializer()
    {
        var constant = new EvolvableConstantSymbol(new UniformDoubleDistribution(10, 14), new ResampleInitialNumericPerturbation());
        var searchSpace = new ExpressionTreeSearchSpace(1, 1, [constant]);

        var result = NodeReplacementMutation.Mutate(Variable("x0").Build(), new SequenceRandomNumberGenerator(0.0, 0.75), searchSpace);

        result.ToInfixString().ShouldBe("13");
        result.Root.Symbol.ShouldBe(constant);
        searchSpace.Contains(result).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesOperationWithAnAllowedSymbolOfTheSameArity()
    {
        var parent = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();
        var searchSpace = new ExpressionTreeSearchSpace(20, 10, Symbols.BasicArithmetic, ["x0", "x1"], [new FixedConstantSymbol(2.0)]);
        var mutant = NodeReplacementMutation.Mutate(parent, new SequenceRandomNumberGenerator(0.0, 0.3), searchSpace);

        parent.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        mutant.ToInfixString().ShouldBe("(x0 - (2 * x1))");
        mutant.EvaluateSingleRow(("x0", 1.0), ("x1", 3.0)).ShouldBe(-5.0);
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesVariableWithAnAllowedVariable()
    {
        var searchSpace = CreateSearchSpace(["x0", "x1"]);
        var mutant = NodeReplacementMutation.Mutate(Variable("x0").Build(), new SequenceRandomNumberGenerator(0.0, 0.0, 0.9), searchSpace);

        mutant.ToInfixString().ShouldBe("x1");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_UsesTheInstanceEntryPoint()
    {
        var searchSpace = new ExpressionTreeSearchSpace(2, 2, [Symbols.Logarithm, Symbols.SquareRoot, new VariableSymbol(["x0"])]);

        var mutant = new NodeReplacementMutator().Mutate(Sqrt(Variable("x0")).Build(), new SequenceRandomNumberGenerator(0.0, 0.0), searchSpace);

        mutant.ToInfixString().ShouldBe("log(x0)");
        mutant.EvaluateSingleRow(("x0", Math.E)).ShouldBe(1.0, tolerance: 1e-12);
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_OnlySelectsSymbolsAllowedByTheSearchSpace()
    {
        var searchSpace = new ExpressionTreeSearchSpace(3, 2, [Symbols.Addition, Symbols.Multiplication, new FixedConstantSymbol(1.0)]);
        var mutant = NodeReplacementMutation.Mutate((FixedConstant(1.0) * FixedConstant(1.0)).Build(), new SequenceRandomNumberGenerator(0.0, 0.0), searchSpace);

        mutant.ToInfixString().ShouldBe("(1 + 1)");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_AllowsNoChangeWhenTheSameVariableIsSampled()
    {
        var parent = Variable("x0").Build();
        var searchSpace = CreateSearchSpace(["x0", "x1"]);
        var mutant = NodeReplacementMutation.Mutate(parent, new SequenceRandomNumberGenerator(0.0, 0.0, 0.0), searchSpace);

        mutant.ToInfixString().ShouldBe("x0");
        searchSpace.Contains(mutant).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesFixedConstantsWithAllowedVariables()
    {
        var variableOnly = new ExpressionTreeSearchSpace(1, 1, [new VariableSymbol(["x0"])]);

        var variable = NodeReplacementMutation.Mutate(FixedConstant(100.0).Build(), new SequenceRandomNumberGenerator(0.0, 0.0), variableOnly);

        variable.ToInfixString().ShouldBe("x0");
        variableOnly.Contains(variable).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesVariablesWithEvolvableConstants()
    {
        var constant = new EvolvableConstantSymbol(new UniformDoubleDistribution(10.0, 14.0), new ResampleInitialNumericPerturbation());
        var constants = new ExpressionTreeSearchSpace(1, 1, [constant]);

        var numeric = NodeReplacementMutation.Mutate(Variable("x0").Build(), new SequenceRandomNumberGenerator(0.0, 0.75), constants);

        numeric.ToInfixString().ShouldBe("13");
        constants.Contains(numeric).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_UsesVariableTerminalsWhenOnlyVariablesAreAvailable()
    {
        var variables = new ExpressionTreeSearchSpace(1, 1, [new VariableSymbol(["x0", "x1"])]);

        var result = NodeReplacementMutation.Mutate(Variable("x0").Build(), new SequenceRandomNumberGenerator(0.0, 0.9), variables);

        result.ToInfixString().ShouldBe("x1");
        variables.Contains(result).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_UsesEvolvableConstantTerminalsWhenOnlyTheyAreAvailable()
    {
        var constant = new EvolvableConstantSymbol(new UniformDoubleDistribution(10.0, 14.0), new ResampleInitialNumericPerturbation());
        var constants = new ExpressionTreeSearchSpace(1, 1, [constant]);

        var result = NodeReplacementMutation.Mutate(FixedConstant(1.0).Build(), new SequenceRandomNumberGenerator(0.0, 0.5), constants);

        result.ToInfixString().ShouldBe("12");
        constants.Contains(result).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_PreservesOperationArity()
    {
        var searchSpace = new ExpressionTreeSearchSpace(10, 5, [new AdditionSymbol(), new SubtractionSymbol(), new VariableSymbol(["x0"])]);

        var result = NodeReplacementMutation.Mutate((Variable("x0") + Variable("x0")).Build(), new SequenceRandomNumberGenerator(0.0, 0.9), searchSpace);

        result.Root.Symbol.ShouldBe(new SubtractionSymbol());
        searchSpace.Contains(result).ShouldBeTrue();
    }

    private static ExpressionTreeSearchSpace CreateSearchSpace(IReadOnlyList<string> variables) =>
        new(20, 10, Symbols.BasicArithmetic, variables);
}
