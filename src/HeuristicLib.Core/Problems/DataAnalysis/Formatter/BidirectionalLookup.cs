using System.Collections;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public class BidirectionalLookup<TFirst, TSecond> where TFirst : notnull where TSecond : notnull
{
  private readonly Dictionary<TFirst, HashSet<TSecond>> firstToSecond;
  private readonly Dictionary<TSecond, HashSet<TFirst>> secondToFirst;

  public BidirectionalLookup()
  {
    firstToSecond = new Dictionary<TFirst, HashSet<TSecond>>();
    secondToFirst = new Dictionary<TSecond, HashSet<TFirst>>();
  }

  public BidirectionalLookup(IEqualityComparer<TFirst> firstComparer)
  {
    firstToSecond = new Dictionary<TFirst, HashSet<TSecond>>(firstComparer);
    secondToFirst = new Dictionary<TSecond, HashSet<TFirst>>();
  }

  public BidirectionalLookup(IEqualityComparer<TSecond> secondComparer)
  {
    firstToSecond = new Dictionary<TFirst, HashSet<TSecond>>();
    secondToFirst = new Dictionary<TSecond, HashSet<TFirst>>(secondComparer);
  }

  public BidirectionalLookup(IEqualityComparer<TFirst> firstComparer, IEqualityComparer<TSecond> secondComparer)
  {
    firstToSecond = new Dictionary<TFirst, HashSet<TSecond>>(firstComparer);
    secondToFirst = new Dictionary<TSecond, HashSet<TFirst>>(secondComparer);
  }

  private sealed class StorableGrouping<TKey, TValue> : IGrouping<TKey, TValue>
  {
    private readonly HashSet<TValue> values;

    public StorableGrouping(TKey key, IEnumerable<TValue> values, IEqualityComparer<TValue> comparer)
    {
      Key = key;
      this.values = new HashSet<TValue>(values, comparer);
    }

    public TKey Key { get; }

    public IEnumerator<TValue> GetEnumerator() => values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }

  #region Properties

  public int CountFirst => firstToSecond.Count;

  public int CountSecond => secondToFirst.Count;

  public IEnumerable<TFirst> FirstKeys => firstToSecond.Keys.AsEnumerable();

  public IEnumerable<TSecond> SecondKeys => secondToFirst.Keys.AsEnumerable();

  public IEnumerable<IGrouping<TFirst, TSecond>> FirstEnumerable => firstToSecond.Select(x => new StorableGrouping<TFirst, TSecond>(x.Key, x.Value, secondToFirst.Comparer));

  public IEnumerable<IGrouping<TSecond, TFirst>> SecondEnumerable => secondToFirst.Select(x => new StorableGrouping<TSecond, TFirst>(x.Key, x.Value, firstToSecond.Comparer));

  #endregion

  #region Methods

  public void Add(TFirst firstValue, TSecond secondValue)
  {
    if (!firstToSecond.TryGetValue(firstValue, out var firstSet)) {
      firstSet = new HashSet<TSecond>(secondToFirst.Comparer);
      firstToSecond[firstValue] = firstSet;
    }

    if (!secondToFirst.TryGetValue(secondValue, out var secondSet)) {
      secondSet = new HashSet<TFirst>(firstToSecond.Comparer);
      secondToFirst[secondValue] = secondSet;
    }

    firstSet.Add(secondValue);
    secondSet.Add(firstValue);
  }

  public void AddRangeFirst(TFirst firstValue, IEnumerable<TSecond> secondValues)
  {
    if (!firstToSecond.TryGetValue(firstValue, out var firstSet)) {
      firstSet = new HashSet<TSecond>(secondToFirst.Comparer);
      firstToSecond[firstValue] = firstSet;
    }

    foreach (var s in secondValues) {
      if (!secondToFirst.TryGetValue(s, out var secondSet)) {
        secondSet = new HashSet<TFirst>(firstToSecond.Comparer);
        secondToFirst[s] = secondSet;
      }

      firstSet.Add(s);
      secondSet.Add(firstValue);
    }
  }

  public void AddRangeSecond(TSecond secondValue, IEnumerable<TFirst> firstValues)
  {
    if (!secondToFirst.TryGetValue(secondValue, out var secondSet)) {
      secondSet = new HashSet<TFirst>(firstToSecond.Comparer);
      secondToFirst[secondValue] = secondSet;
    }

    foreach (var f in firstValues) {
      if (!firstToSecond.TryGetValue(f, out var firstSet)) {
        firstSet = new HashSet<TSecond>(secondToFirst.Comparer);
        firstToSecond[f] = firstSet;
      }

      firstSet.Add(secondValue);
      secondSet.Add(f);
    }
  }

  public bool ContainsFirst(TFirst firstValue) => firstToSecond.ContainsKey(firstValue);

  public bool ContainsSecond(TSecond secondValue) => secondToFirst.ContainsKey(secondValue);

  public IEnumerable<TSecond> GetByFirst(TFirst firstValue) => firstToSecond[firstValue];

  public IEnumerable<TFirst> GetBySecond(TSecond secondValue) => secondToFirst[secondValue];

  public void SetByFirst(TFirst firstValue, IEnumerable<TSecond> secondValues)
  {
    RemoveByFirst(firstValue);
    AddRangeFirst(firstValue, secondValues);
  }

  public void SetBySecond(TSecond secondValue, IEnumerable<TFirst> firstValues)
  {
    RemoveBySecond(secondValue);
    AddRangeSecond(secondValue, firstValues);
  }

  public void RemovePair(TFirst first, TSecond second)
  {
    if (!ContainsFirst(first) || !ContainsSecond(second)) {
      return;
    }
    firstToSecond[first].Remove(second);
    if (!firstToSecond[first].Any()) {
      firstToSecond.Remove(first);
    }
    secondToFirst[second].Remove(first);
    if (!secondToFirst[second].Any()) {
      secondToFirst.Remove(second);
    }
  }

  public void RemoveByFirst(TFirst firstValue)
  {
    if (!ContainsFirst(firstValue)) {
      return;
    }
    var secondValues = firstToSecond[firstValue].ToArray();
    firstToSecond.Remove(firstValue);
    foreach (var s in secondValues) {
      secondToFirst[s].Remove(firstValue);
      if (!secondToFirst[s].Any()) {
        secondToFirst.Remove(s);
      }
    }
  }

  public void RemoveBySecond(TSecond secondValue)
  {
    if (!ContainsSecond(secondValue)) {
      return;
    }
    var firstValues = secondToFirst[secondValue].ToArray();
    secondToFirst.Remove(secondValue);
    foreach (var f in firstValues) {
      firstToSecond[f].Remove(secondValue);
      if (!firstToSecond[f].Any()) {
        firstToSecond.Remove(f);
      }
    }
  }

  public void Clear()
  {
    firstToSecond.Clear();
    secondToFirst.Clear();
  }

  #endregion

}
