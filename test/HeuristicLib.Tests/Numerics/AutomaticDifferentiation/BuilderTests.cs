using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;
using Instruction = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation.Instruction;

namespace HEAL.HeuristicLib.Tests.Numerics.AutomaticDifferentiation;

public sealed class BuilderTests
{
    [Fact]
    public void BuildCompilesIndexedProgramAndMetadata()
    {
        var builder = new Builder();
        var input = builder.Input();
        var parameter = builder.Parameter();
        var constant = builder.Constant(2.0);
        var product = builder.Multiply(parameter, input);
        var root = builder.Add(product, constant);

        var program = builder.Build(root);

        program.Instructions.ToArray().ShouldBe([
            new Instruction(Operation.Variable, -1, -1, 0, -1, -1),
            new Instruction(Operation.Parameter, -1, -1, 0, -1, 0),
            new Instruction(Operation.Constant, -1, -1, 0, -1, -1),
            new Instruction(Operation.Multiply, 1, 0, -1, 0, 1),
            new Instruction(Operation.Add, 3, 2, -1, 1, 2),
        ]);
        program.Constants.ToArray().ShouldBe([2.0]);
        program.ParameterInstructionIndices.ToArray().ShouldBe([1]);
        program.InstructionCount.ShouldBe(5);
        program.InputCount.ShouldBe(1);
        program.ParameterCount.ShouldBe(1);
        program.VectorPrimalSlotCount.ShouldBe(2);
        program.AdjointSlotCount.ShouldBe(3);
        program.RootInstructionIndex.ShouldBe(4);
    }

    [Fact]
    public void BuildPreservesSharedSubexpressions()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var shared = builder.Negate(parameter);
        var root = builder.Add(shared, shared);

        var program = builder.Build(root);

        program.InstructionCount.ShouldBe(3);
        var rootInstruction = program.Instructions[program.RootInstructionIndex];
        rootInstruction.LeftOperand.ShouldBe(1);
        rootInstruction.RightOperand.ShouldBe(1);
        program.ParameterInstructionIndices.ToArray().ShouldBe([0]);
    }

    [Fact]
    public void BuildDiscardsUnreachableIntermediateCalculations()
    {
        var builder = new Builder();
        var input = builder.Input();
        var parameter = builder.Parameter();
        var deadLeft = builder.Constant(1.0);
        var deadRight = builder.Constant(2.0);
        _ = builder.Add(deadLeft, deadRight);
        var root = builder.Multiply(parameter, input);

        var program = builder.Build(root);

        program.Instructions.ToArray().Select(instruction => instruction.Operation)
            .ShouldBe([Operation.Variable, Operation.Parameter, Operation.Multiply]);
        program.Constants.Length.ShouldBe(0);
        program.Instructions[2].LeftOperand.ShouldBe(1);
        program.Instructions[2].RightOperand.ShouldBe(0);
    }

    [Fact]
    public void BuildRejectsUnreachableInputs()
    {
        var builder = new Builder();
        _ = builder.Input();
        var root = builder.Constant(1.0);

        var exception = Should.Throw<InvalidOperationException>(() => builder.Build(root));

        exception.Message.ShouldBe("Input 0 is not reachable from the program root.");
    }

    [Fact]
    public void BuildRejectsUnreachableParameters()
    {
        var builder = new Builder();
        _ = builder.Parameter();
        var root = builder.Constant(1.0);

        var exception = Should.Throw<InvalidOperationException>(() => builder.Build(root));

        exception.Message.ShouldBe("Parameter 0 is not reachable from the program root.");
    }

    [Fact]
    public void BuildMapsParametersInCreationOrder()
    {
        var builder = new Builder();
        var first = builder.Parameter();
        var second = builder.Parameter();
        var root = builder.Subtract(second, first);

        var program = builder.Build(root);

        program.ParameterInstructionIndices.ToArray().ShouldBe([0, 1]);
        program.Instructions[0].PayloadIndex.ShouldBe(0);
        program.Instructions[1].PayloadIndex.ShouldBe(1);
    }

    [Fact]
    public void BuildMapsInputsInCreationOrder()
    {
        var builder = new Builder();
        var first = builder.Input();
        var second = builder.Input();
        var root = builder.Subtract(second, first);

        var program = builder.Build(root);

        program.InputCount.ShouldBe(2);
        program.Instructions[0].PayloadIndex.ShouldBe(0);
        program.Instructions[1].PayloadIndex.ShouldBe(1);
    }

    [Fact]
    public void BuildRetainsEveryAuthoredOperation()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var constant = builder.Constant(1.0);
        var add = builder.Add(parameter, constant);
        var subtract = builder.Subtract(add, constant);
        var multiply = builder.Multiply(subtract, constant);
        var divide = builder.Divide(multiply, constant);
        var negate = builder.Negate(divide);
        var exp = builder.Exp(negate);
        var log = builder.Log(exp);
        var sin = builder.Sin(log);
        var cos = builder.Cos(sin);
        var tan = builder.Tan(cos);
        var root = builder.Tanh(tan);

        var program = builder.Build(root);

        program.Instructions.ToArray().Select(instruction => instruction.Operation).ShouldBe([
            Operation.Parameter, Operation.Constant, Operation.Add, Operation.Subtract, Operation.Multiply,
            Operation.Divide, Operation.Negate, Operation.Exp, Operation.Log, Operation.Sin, Operation.Cos,
            Operation.Tan, Operation.Tanh,
        ]);
        program.Constants.ToArray().ShouldBe([1.0]);
        program.VectorPrimalSlotCount.ShouldBe(0);
        program.AdjointSlotCount.ShouldBe(12);
    }

    [Fact]
    public void BuildAssignsVectorSlotsWithoutAdjointsForInputOnlyCalculations()
    {
        var builder = new Builder();
        var input = builder.Input();
        var root = builder.Sin(input);

        var program = builder.Build(root);

        program.VectorPrimalSlotCount.ShouldBe(1);
        program.AdjointSlotCount.ShouldBe(0);
        program.Instructions[1].VectorPrimalSlot.ShouldBe(0);
        program.Instructions[1].AdjointSlot.ShouldBe(-1);
        program.Instructions[1].DependsOnInput.ShouldBeTrue();
        program.Instructions[1].DependsOnParameter.ShouldBeFalse();
    }

    [Fact]
    public void BuildDoesNotOptimizeAuthoredGraph()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var firstNegation = builder.Negate(parameter);
        var secondNegation = builder.Negate(parameter);
        var zero = builder.Constant(0.0);
        var sum = builder.Add(firstNegation, secondNegation);
        var root = builder.Add(sum, zero);

        var program = builder.Build(root);

        program.Instructions.ToArray().Select(instruction => instruction.Operation).ShouldBe([
            Operation.Parameter, Operation.Negate, Operation.Negate, Operation.Constant, Operation.Add, Operation.Add,
        ]);
    }

    [Fact]
    public void OperationRejectsValueFromAnotherBuilder()
    {
        var builder = new Builder();
        var otherBuilder = new Builder();
        var local = builder.Constant(1.0);
        var foreign = otherBuilder.Constant(2.0);

        var exception = Should.Throw<ArgumentException>(() => builder.Add(local, foreign));

        exception.ParamName.ShouldBe("right");
    }

    [Fact]
    public void OperationRejectsDefaultValue()
    {
        var builder = new Builder();

        var exception = Should.Throw<ArgumentException>(() => builder.Negate(default));

        exception.ParamName.ShouldBe("value");
    }

    [Fact]
    public void BuildRejectsRootFromAnotherBuilder()
    {
        var builder = new Builder();
        var otherBuilder = new Builder();
        var foreignRoot = otherBuilder.Constant(1.0);

        var exception = Should.Throw<ArgumentException>(() => builder.Build(foreignRoot));

        exception.ParamName.ShouldBe("root");
    }

    [Fact]
    public void BuilderRejectsUseAfterSuccessfulBuild()
    {
        var builder = new Builder();
        var root = builder.Constant(1.0);
        _ = builder.Build(root);

        Should.Throw<InvalidOperationException>(() => builder.Input());
        Should.Throw<InvalidOperationException>(() => builder.Build(root));
    }
}
