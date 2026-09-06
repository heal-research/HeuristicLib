using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed class NoisyFlowQuadraticAssignmentProblem
    : DynamicProblem<Permutation, PermutationSearchSpace>
{
    private readonly QuadraticAssignmentProblemData baseProblemData;

    private readonly double[,] noisyFlows; // current state
    private readonly double sigma;

    public NoisyFlowQuadraticAssignmentProblem(
        QuadraticAssignmentProblemData problemData,
        IRandomNumberGenerator environmentRandom,
        double sigma,
        IEpochSchedule? epochSchedule = null,
        UpdatePolicy updatePolicy = UpdatePolicy.AfterEachEvaluation
    ) : base(SingleObjective.Minimize, new PermutationSearchSpace(problemData.Size), environmentRandom, epochSchedule ?? new EvaluationCountSchedule(int.MaxValue), updatePolicy)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sigma);

        baseProblemData = problemData;
        this.sigma = sigma;
        noisyFlows = new double[baseProblemData.Size, baseProblemData.Size];
        Update();
    }

    protected override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, int epoch)
    {
        var n = baseProblemData.Size;
        var cost = 0.0;

        for (var i = 0; i < n; i++)
        {
            var li = solution[i];
            for (var j = 0; j < n; j++)
            {
                var lj = solution[j];
                cost += noisyFlows[i, j] * baseProblemData.GetDistance(li, lj);
            }
        }

        return cost;
    }

    protected override void Update()
    {
        var n = baseProblemData.Size;

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                noisyFlows[i, j] = baseProblemData.GetFlow(i, j) + EnvironmentRandom.NextNormal(0, sigma);
            }
        }
    }
}
