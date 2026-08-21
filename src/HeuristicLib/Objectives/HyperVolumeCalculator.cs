using MathNet.Numerics.Statistics;

namespace HEAL.HeuristicLib.Objectives;

public static class HyperVolumeCalculator
{
    /// <summary>
    /// The Hypervolume-metric is defined as the Hypervolume enclosed between a given reference point,
    /// that is fixed for every evaluation function and the evaluated front.
    ///
    /// Example:
    /// r is the reference Point at (1|1) and every Point p is part of the evaluated front
    /// The filled Area labeled HV is the 2-dimensional Hypervolume enclosed by this front.
    ///
    /// (0|1)                (1|1)
    ///   +      +-------------r
    ///   |      |###### HV ###|
    ///   |      p------+######|
    ///   |             p+#####|
    ///   |              |#####|
    ///   |              p-+###|
    ///   |                p---+
    ///   |
    ///   +--------------------1
    /// (0|0)                (1|0)
    ///
    ///  Please note that in this example both dimensions are minimized. The reference Point need to be dominated by EVERY point in the evaluated front
    ///
    /// </summary>
    ///
    public static double Calculate(IEnumerable<ObjectiveVector> front, ObjectiveVector referencePoint, ObjectiveDirections maximization)
    {
        var dominatingVectors = GetDominatingVectors(front, referencePoint, maximization);
        if (dominatingVectors.Count == 0)
            return 0;
        if (maximization.Directions.Length == 2)
            return Calculate2D(dominatingVectors, referencePoint, maximization);

        return maximization.Directions.All(x => x == ObjectiveDirection.Minimize)
          ? CalculateMultiDimensional(dominatingVectors, referencePoint)
          : throw new NotSupportedException("Hypervolume calculation for more than two dimensions is supported only with minimization problems.");
    }

    private static List<ObjectiveVector> GetDominatingVectors(IEnumerable<ObjectiveVector> qualities, ObjectiveVector reference, ObjectiveDirections objective)
    {
        return qualities.Where(vec => vec.CompareTo(reference, objective) == DominanceRelation.Dominates).ToList();
    }

    private static double Calculate2D(List<ObjectiveVector> front, ObjectiveVector referencePoint, ObjectiveDirections objective)
    {
        if (front.Count == 0)
            return 0;
        if (referencePoint.Count != 2)
            throw new ArgumentException("ReferencePoint must have exactly two dimensions.");

        var set = front.ToArray();
        if (set.Any(s => s.Count != 2))
            throw new ArgumentException("Points in front must have exactly two dimensions.");

        Array.Sort(set, new DimensionComparer(0, objective));

        double sum = 0;
        for (var i = 0; i < set.Length - 1; i++)
        {
            sum += Math.Abs(set[i][0] - set[i + 1][0]) * Math.Abs(set[i][1] - referencePoint[1]);
        }

        var lastPoint = set[^1];
        sum += Math.Abs(lastPoint[0] - referencePoint[0]) * Math.Abs(lastPoint[1] - referencePoint[1]);

        return sum;
    }

    private sealed class DimensionComparer : IComparer<ObjectiveVector>
    {
        private readonly int dim;
        private readonly int descending;

        public DimensionComparer(int dimension, ObjectiveDirections objective)
        {
            dim = dimension;
            descending = objective.Directions[dimension] == ObjectiveDirection.Maximize ? -1 : 1;
        }

        public DimensionComparer(int dimension, ObjectiveDirection objective)
        {
            dim = dimension;
            descending = objective == ObjectiveDirection.Maximize ? -1 : 1;
        }

        public int Compare(ObjectiveVector? x, ObjectiveVector? y) =>
            ObjectiveValue.Compare(x![dim], y![dim], descending < 0 ? ObjectiveDirection.Maximize : ObjectiveDirection.Minimize);
    }

    private static double CalculateMultiDimensional(IEnumerable<ObjectiveVector> front, ObjectiveVector referencePoint)
    {
        if (referencePoint.Count < 3)
            throw new ArgumentException("ReferencePoint unfit for complex Hypervolume calculation");

        var objectives = referencePoint.Count;
        var fronList = front.OrderBy(x => x, new DimensionComparer(objectives - 1, ObjectiveDirection.Minimize)).ToList();
        // This path is minimization-only, so the region starts at the worst value and every point pulls it down.
        // A finite seed would leave an objective above it untracked.
        var regLow = Enumerable.Repeat(ObjectiveValue.WorstValue(ObjectiveDirection.Minimize).Value, objectives).ToArray();
        foreach (var p in fronList)
        {
            for (var i = 0; i < regLow.Length; i++)
            {
                if (p[i] < regLow[i])
                    regLow[i] = p[i];
            }
        }

        var refPoint = referencePoint.ToArray();
        return Stream(regLow, refPoint, fronList, 0, referencePoint[objectives - 1], (int)Math.Sqrt(fronList.Count), objectives);
    }

    private static double Stream(double[] regionLow, double[] regionUp, List<ObjectiveVector> front, int split, double cover, int sqrtNoPoints, int objectives)
    {
        var coverOld = cover;
        var coverIndex = 0;
        double result = 0;

        var dMeasure = GetMeasure(regionLow, regionUp, objectives);
        if (double.IsNaN(cover))
            return result;

        var coverFound = false;
        while (!coverFound && coverIndex < front.Count)
        {
            if (Covers(front[coverIndex], regionLow, objectives))
            {
                cover = front[coverIndex][objectives - 1];
                result += dMeasure * (coverOld - cover);
                coverFound = true;
            }
            else
            {
                coverIndex++;
            }
        }

#pragma warning disable S1244
        for (var c = coverIndex; c > 0; c--)
            if (front[c - 1][objectives - 1] == cover)
                coverIndex--;
#pragma warning restore S1244

        if (coverIndex == 0)
            return result;

        var allPiles = true;
        var piles = new int[coverIndex];
        for (var i = 0; i < coverIndex; i++)
        {
            piles[i] = IsPile(front[i], regionLow, objectives);
            if (piles[i] != -1)
                continue;

            allPiles = false;
            break;
        }

        if (allPiles)
        {
            var trellis = new double[regionUp.Length];
            for (var j = 0; j < trellis.Length; j++)
                trellis[j] = regionUp[j];
            double next;
            var i = 0;
            var reachedCover = false;
            do
            {
                var current = front[i][objectives - 1];
#pragma warning disable S1244
                do
                {
                    if (front[i][piles[i]] < trellis[piles[i]])
                        trellis[piles[i]] = front[i][piles[i]];
                    i++;
                    if (i < coverIndex)
                        next = front[i][objectives - 1];
                    else
                    {
                        next = cover;
                        reachedCover = true;
                        break;
                    }
                } while (next == current);
#pragma warning restore S1244

                result += ComputeTrellis(regionLow, regionUp, trellis, objectives) * (next - current);
            } while (!reachedCover);
        }
        else
        {
            double? bound = null;
            var boundaries = new double[coverIndex];
            var noBoundaries = new double[coverIndex];
            var boundIdx = 0;
            var noBoundIdx = 0;

            do
            {
                for (var i = 0; i < coverIndex; i++)
                {
                    var contained = ContainsBoundary(front[i], regionLow, split);
                    switch (contained)
                    {
                        case 0:
                            boundaries[boundIdx++] = front[i][split];
                            break;
                        case 1:
                            noBoundaries[noBoundIdx++] = front[i][split];
                            break;
                    }
                }

                if (boundIdx > 0)
                    bound = GetMedian(boundaries, boundIdx);
                else if (noBoundIdx > sqrtNoPoints)
                    bound = GetMedian(noBoundaries, noBoundIdx);
                else
                    split++;
            } while (!bound.HasValue);

            var pointsChildLow = new List<ObjectiveVector>();
            var pointsChildUp = new List<ObjectiveVector>();
            var regionUpC = new double[regionUp.Length];
            for (var j = 0; j < regionUpC.Length; j++)
                regionUpC[j] = regionUp[j];
            var regionLowC = new double[regionLow.Length];
            for (var j = 0; j < regionLowC.Length; j++)
                regionLowC[j] = regionLow[j];

            for (var i = 0; i < coverIndex; i++)
            {
                if (PartCovers(front[i], regionUpC, objectives))
                    pointsChildUp.Add(front[i]);
                if (PartCovers(front[i], regionUp, objectives))
                    pointsChildLow.Add(front[i]);
            }
            //this could/should be done in Parallel

            if (pointsChildUp.Count > 0)
                result += Stream(regionLow, regionUpC, pointsChildUp, split, cover, sqrtNoPoints, objectives);
            if (pointsChildLow.Count > 0)
                result += Stream(regionLowC, regionUp, pointsChildLow, split, cover, sqrtNoPoints, objectives);
        }

        return result;
    }

    private static double GetMedian(double[] vector, int length)
    {
        return vector.Take(length).Median();
    }

    private static double ComputeTrellis(double[] regionLow, double[] regionUp, double[] trellis, int objectives)
    {
        var bs = new bool[objectives - 1];
        for (var i = 0; i < bs.Length; i++)
            bs[i] = true;

        double result = 0;
        var noSummands = BinaryToInt(bs);
        for (uint i = 1; i <= noSummands; i++)
        {
            double summand = 1;
            IntToBinary(i, bs);
            var oneCounter = 0;
            for (var j = 0; j < objectives - 1; j++)
            {
                if (bs[j])
                {
                    summand *= regionUp[j] - trellis[j];
                    oneCounter++;
                }
                else
                {
                    summand *= regionUp[j] - regionLow[j];
                }
            }

            if (oneCounter % 2 == 0)
                result -= summand;
            else
                result += summand;
        }

        return result;
    }

    private static void IntToBinary(uint i, bool[] bs)
    {
        for (var j = 0; j < bs.Length; j++)
            bs[j] = false;
        var rest = i;
        var idx = 0;
        while (rest != 0)
        {
            bs[idx] = rest % 2 == 1;
            rest = rest / 2;
            idx++;
        }
    }

    private static uint BinaryToInt(bool[] bs)
    {
        uint result = 0;
        for (var i = 0; i < bs.Length; i++)
        {
            result += bs[i] ? ((uint)1 << i) : 0;
        }

        return result;
    }

    private static int IsPile(ObjectiveVector cuboid, double[] regionLow, int objectives)
    {
        var pile = cuboid.Count;
        for (var i = 0; i < objectives - 1; i++)
        {
            if (cuboid[i] <= regionLow[i])
            {
                continue;
            }

            if (pile != objectives)
                return 1;
            pile = i;
        }

        return pile;
    }

    private static double GetMeasure(double[] regionLow, double[] regionUp, int objectives)
    {
        double volume = 1;
        for (var i = 0; i < objectives - 1; i++)
        {
            volume *= (regionUp[i] - regionLow[i]);
        }

        return volume;
    }

    private static int ContainsBoundary(ObjectiveVector cub, double[] regionLow, int split)
    {
        if (regionLow[split] >= cub[split])
            return -1;
        for (var j = 0; j < split; j++)
        {
            if (regionLow[j] < cub[j])
                return 1;
        }

        return 0;
    }

    private static bool PartCovers(ObjectiveVector v, double[] regionUp, int objectives)
    {
        for (var i = 0; i < objectives - 1; i++)
        {
            if (v[i] >= regionUp[i])
                return false;
        }

        return true;
    }

    private static bool Covers(ObjectiveVector v, double[] regionLow, int objectives)
    {
        for (var i = 0; i < objectives - 1; i++)
        {
            if (v[i] > regionLow[i])
                return false;
        }

        return true;
    }
}
