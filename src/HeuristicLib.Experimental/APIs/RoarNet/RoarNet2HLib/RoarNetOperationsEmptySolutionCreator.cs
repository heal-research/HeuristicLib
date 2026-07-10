using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed record RoarNetOperationsEmptySolutionCreator : StatelessCreator<Solution, RoarNetOperationsSearchSpace, RoarNetOperationsProblem>
{
    public override IReadOnlyList<Solution> Create(int count, IRandomNumberGenerator random, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem)
        => Enumerable.Range(0, count).Select(_ => operationsProblem.Operations.empty_solution(operationsProblem.RoarNetProblemInstance)).ToArray();
}
