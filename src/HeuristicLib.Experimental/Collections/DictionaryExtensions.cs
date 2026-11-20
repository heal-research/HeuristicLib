namespace HEAL.HeuristicLib.Collections;

internal static class DictionaryExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        internal TValue GetOrAdd(TKey key, TValue defaultValue)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            dictionary[key] = defaultValue;
            return defaultValue;
        }
    }
}
