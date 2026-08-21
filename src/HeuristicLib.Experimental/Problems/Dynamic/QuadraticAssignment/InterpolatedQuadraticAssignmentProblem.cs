using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed class InterpolatedQuadraticAssignmentProblem
    : DynamicProblem<Permutation, PermutationSearchSpace>
{
    private readonly QuadraticAssignmentProblemData a;
    private readonly double alphaStep;
    private readonly QuadraticAssignmentProblemData b;
    private readonly double[,] currentDistances;

    private readonly double[,] currentFlows;
    private readonly bool interpolateDistances;
    private readonly bool pingPong;
    private int alphaDirection = 1;

    public InterpolatedQuadraticAssignmentProblem(
        QuadraticAssignmentProblemData a,
        QuadraticAssignmentProblemData b,
        IRandomNumberGenerator environmentRandom,
        double alphaStart = 0.0,
        double alphaStep = 0.01,
        bool interpolateDistances = false,
        bool pingPong = true,
        UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
        int epochLength = int.MaxValue
    ) : base(SingleObjective.Minimize, new PermutationSearchSpace(a.Size), environmentRandom, updatePolicy, epochLength)
    {
        if (a.Size != b.Size)
        {
            throw new ArgumentException("Instances must have same size.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(alphaStart, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alphaStart, 1.0);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alphaStep);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alphaStep, 1.0);

        this.a = a;
        this.b = b;
        this.interpolateDistances = interpolateDistances;
        Alpha = alphaStart;
        this.alphaStep = alphaStep;
        this.pingPong = pingPong;

        currentFlows = new double[a.Size, a.Size];
        currentDistances = new double[a.Size, a.Size];

        RebuildCurrentMatrices();
    }

    public double Alpha { get; private set; }

    public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random,
                                             EvaluationTiming timing)
    {
        var n = a.Size;
        var cost = 0.0;

        for (var i = 0; i < n; i++)
        {
            var li = solution[i];
            for (var j = 0; j < n; j++)
            {
                var lj = solution[j];
                cost += currentFlows[i, j] * currentDistances[li, lj];
            }
        }

        return cost;
    }

    protected override void Update()
    {
        var next = Alpha + alphaDirection * alphaStep;

        if (!pingPong)
        {
            if (next > 1.0)
            {
                next -= Math.Floor(next);
            }

            if (next < 0.0)
            {
                next -= Math.Floor(next);
            }

            Alpha = next;
        }
        else
        {
            while (next is < 0.0 or > 1.0)
            {
                if (next > 1.0)
                {
                    next = 2.0 - next;
                    alphaDirection = -1;
                    continue;
                }

                next = -next;
                alphaDirection = 1;
            }

            Alpha = next;
        }

        RebuildCurrentMatrices();
    }

    private void RebuildCurrentMatrices()
    {
        LerpFlowsInto(currentFlows, a, b, Alpha);

        if (interpolateDistances)
        {
            LerpDistancesInto(currentDistances, a, b, Alpha);
        }
        else
        {
            CopyDistancesInto(currentDistances, a);
        }
    }

    private static void LerpFlowsInto(double[,] destination, IQuadraticAssignmentProblemData x, IQuadraticAssignmentProblemData y, double t)
    {
        var s = 1.0 - t;

        for (var i = 0; i < destination.GetLength(0); i++)
        {
            for (var j = 0; j < destination.GetLength(1); j++)
            {
                destination[i, j] = s * x.GetFlow(i, j) + t * y.GetFlow(i, j);
            }
        }
    }

    private static void LerpDistancesInto(double[,] destination, IQuadraticAssignmentProblemData x, IQuadraticAssignmentProblemData y, double t)
    {
        var s = 1.0 - t;

        for (var i = 0; i < destination.GetLength(0); i++)
        {
            for (var j = 0; j < destination.GetLength(1); j++)
            {
                destination[i, j] = s * x.GetDistance(i, j) + t * y.GetDistance(i, j);
            }
        }
    }

    private static void CopyDistancesInto(double[,] destination, IQuadraticAssignmentProblemData source)
    {
        for (var i = 0; i < destination.GetLength(0); i++)
        {
            for (var j = 0; j < destination.GetLength(1); j++)
            {
                destination[i, j] = source.GetDistance(i, j);
            }
        }
    }
}
