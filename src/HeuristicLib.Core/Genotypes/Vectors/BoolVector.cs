using System.Collections;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public class BoolVector : IReadOnlyList<bool>
{
  private readonly bool[] elements;

  public BoolVector(IEnumerable<bool> elements) => this.elements = elements.ToArray();

  public BoolVector(bool value) => elements = [value];

  public bool this[Index index] => elements[index];

  public bool this[int index] => elements[index];

  public IEnumerator<bool> GetEnumerator() => ((IEnumerable<bool>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public int Count => elements.Length;

  public static implicit operator BoolVector(bool value) => new(value);

  public bool Contains(bool value) => elements.Contains(value);

  public static bool AreCompatible(BoolVector a, BoolVector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

  public static int BroadcastLength(BoolVector a, BoolVector b) => Math.Max(a.Count, b.Count);

  // Boolean operations
  public static BoolVector And(BoolVector a, BoolVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for logical operations");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue && bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector Or(BoolVector a, BoolVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for logical operations");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue || bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector Xor(BoolVector a, BoolVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for logical operations");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue ^ bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector Not(BoolVector a)
  {
    var result = new bool[a.Count];

    for (var i = 0; i < a.Count; i++) {
      result[i] = !a[i];
    }

    return new BoolVector(result);
  }

  // Operator overloads
  public static BoolVector operator &(BoolVector a, BoolVector b) => And(a, b);
  public static BoolVector operator |(BoolVector a, BoolVector b) => Or(a, b);
  public static BoolVector operator ^(BoolVector a, BoolVector b) => Xor(a, b);
  public static BoolVector operator !(BoolVector a) => Not(a);

  // Utility methods
  public bool All()
  {
    foreach (var element in elements) {
      if (!element) {
        return false;
      }
    }

    return true;
  }

  public bool Any()
  {
    foreach (var element in elements) {
      if (element) {
        return true;
      }
    }

    return false;
  }

  public int TrueCount()
  {
    var count = 0;
    foreach (var element in elements) {
      if (element) {
        count++;
      }
    }

    return count;
  }

  public override string ToString() => $"[{string.Join(", ", elements.Select(b => b ? "True" : "False"))}]";
}
