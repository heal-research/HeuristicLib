using HEAL.HeuristicLib.APIs.RoarNet;

namespace HEAL.HeuristicLib.Tests.Experimental.APIs.RoarNet;

public sealed class TuringSolutionTests
{
    [Fact]
    public void Apply_LeavesTheOriginalTapeUnchanged()
    {
        var original = new TuringSolution(0, 0, [1], 0);

        var replaced = original.Apply(new TuringMove(1, 2, Right: true));
        var appended = replaced.Apply(new TuringMove(2, 3, Right: true));

        original.Tape.ShouldBe([1]);
        replaced.Tape.ShouldBe([2]);
        appended.Tape.ShouldBe([2, 3]);
    }

    [Fact]
    public void OperationsApply_LeavesTheOriginalTapeUnchanged()
    {
        var original = new TuringOperations.Solution(0, 0, [1], 0);

        var replaced = original.Apply(new TuringOperations.Move(1, 2, Right: true));
        var appended = replaced.Apply(new TuringOperations.Move(2, 3, Right: true));

        original.Tape.ShouldBe([1]);
        replaced.Tape.ShouldBe([2]);
        appended.Tape.ShouldBe([2, 3]);
    }

    [Fact]
    public void CopySolution_ReturnsAnEqualDistinctSolution()
    {
        var solution = new TuringSolution(1, 1, [1, 2], 3);
        var operationsSolution = new TuringOperations.Solution(1, 1, [1, 2], 3);

        var copy = new TuringProblem([]).copy_solution(solution);
        var operationsCopy = new TuringOperations([]).copy_solution(operationsSolution);

        copy.ShouldNotBeSameAs(solution);
        copy.ShouldBe(solution);
        operationsCopy.ShouldNotBeSameAs(operationsSolution);
        operationsCopy.ShouldBe(operationsSolution);
    }
}
