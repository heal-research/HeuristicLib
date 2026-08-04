using System.Numerics;
using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Tests.Scenarios.Numerics.AutomaticDifferentiation;

public sealed class ExecutionMemoryAndConcurrencyScenarios
{
    [Fact]
    public void EvaluationAllocatesNoManagedMemoryAfterExecutionCreation()
    {
        var program = CreateProgramCoveringVectorizedReverseRules();
        const int rowCount = 257;
        var input = Enumerable.Range(0, rowCount).Select(rowIndex => 0.1 + (0.001 * rowIndex)).ToArray();
        using var execution = program.CreateExecution([input], rowCount, batchCapacity: 64);
        double[] parameters = [0.4, 0.7, 1.3];
        var outputs = new double[rowCount];
        var jacobian = new double[parameters.Length * rowCount];

        for (var iteration = 0; iteration < 100; iteration++)
        {
            execution.Evaluate(parameters, outputs);
            execution.EvaluateWithJacobian(parameters, outputs, jacobian);
        }

        var allocatedBeforeForward = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 100; iteration++)
            execution.Evaluate(parameters, outputs);
        var forwardAllocations = GC.GetAllocatedBytesForCurrentThread() - allocatedBeforeForward;

        var allocatedBeforeJacobian = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 100; iteration++)
            execution.EvaluateWithJacobian(parameters, outputs, jacobian);
        var jacobianAllocations = GC.GetAllocatedBytesForCurrentThread() - allocatedBeforeJacobian;

        forwardAllocations.ShouldBe(0);
        jacobianAllocations.ShouldBe(0);
    }

    [Fact]
    public async Task SeparateExecutionsReuseOneProgramConcurrently()
    {
        var program = CreateProgramCoveringVectorizedReverseRules();
        var workerCount = Math.Max(2, Math.Min(8, Environment.ProcessorCount));
        var rowCount = (2 * Vector<double>.Count) + 1;
        var inputs = Enumerable.Range(0, workerCount).Select(worker => Enumerable.Range(0, rowCount).Select(rowIndex => 0.1 + (0.01 * worker) + (0.002 * rowIndex)).ToArray()).ToArray();
        using var start = new ManualResetEventSlim();
        var tasks = Enumerable.Range(0, workerCount).Select(worker => Task.Factory.StartNew(() =>
        {
            using var execution = program.CreateExecution([inputs[worker]], rowCount, batchCapacity: Vector<double>.Count + 1);
            double[] parameters = [0.4 + (0.01 * worker), 0.7 + (0.02 * worker), 1.3 + (0.03 * worker)];
            var outputs = new double[rowCount];
            var jacobian = new double[parameters.Length * rowCount];
            start.Wait();
            for (var iteration = 0; iteration < 100; iteration++)
                execution.EvaluateWithJacobian(parameters, outputs, jacobian);
            return (Parameters: parameters, Outputs: outputs, Jacobian: jacobian);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        start.Set();
        var results = await Task.WhenAll(tasks);
        for (var worker = 0; worker < workerCount; worker++)
        {
            using var referenceExecution = program.CreateExecution([inputs[worker]], rowCount, batchCapacity: Vector<double>.Count + 1);
            var expectedOutputs = new double[rowCount];
            var expectedJacobian = new double[results[worker].Parameters.Length * rowCount];
            referenceExecution.EvaluateWithJacobian(results[worker].Parameters, expectedOutputs, expectedJacobian);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                results[worker].Outputs[rowIndex].ShouldBe(expectedOutputs[rowIndex], 1e-12);
            for (var jacobianIndex = 0; jacobianIndex < expectedJacobian.Length; jacobianIndex++)
                results[worker].Jacobian[jacobianIndex].ShouldBe(expectedJacobian[jacobianIndex], 1e-12);
        }
    }

    private static Program CreateProgramCoveringVectorizedReverseRules()
    {
        var builder = new Builder();
        var input = builder.Input();
        var first = builder.Parameter();
        var second = builder.Parameter();
        var third = builder.Parameter();
        var firstProduct = builder.Multiply(first, input);
        var secondProduct = builder.Multiply(second, input);
        var shiftedInput = builder.Add(input, builder.Constant(1.0));
        var vectorDenominator = builder.Multiply(third, shiftedInput);
        var trigonometric = builder.Add(builder.Add(builder.Sin(firstProduct), builder.Cos(secondProduct)), builder.Add(builder.Tan(builder.Multiply(builder.Constant(0.1), firstProduct)), builder.Tanh(builder.Multiply(third, input))));
        var divisions = builder.Add(builder.Add(builder.Divide(second, shiftedInput), builder.Divide(firstProduct, third)), builder.Add(builder.Divide(second, vectorDenominator), builder.Divide(firstProduct, vectorDenominator)));
        return builder.Build(builder.Add(trigonometric, divisions));
    }
}
