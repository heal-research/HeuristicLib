namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public static class Extensions
{
    extension(Range r)
    {
        public IEnumerable<int> Enumerate()
        {
            var s = r.Start.Value;
            var e = r.End.Value;

            return s <= e ? Enumerable.Range(s, e - s) : Enumerable.Range(e, s - e).Reverse();
        }

    }
}
