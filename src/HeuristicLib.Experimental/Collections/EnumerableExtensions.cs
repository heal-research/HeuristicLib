namespace HEAL.HeuristicLib.Collections;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<TResult> SelectCircularPairs<TResult>(Func<T, T, TResult> selector)
        {
            T first = default!;
            var count = 0;
            T last = default!;
            foreach (var item in source)
            {
                if (count == 0)
                {
                    first = item;
                }
                else
                {
                    yield return selector(last, item);
                }

                last = item;
                count++;
            }

            if (count > 1)
                yield return selector(last, first);
        }
    }
}
