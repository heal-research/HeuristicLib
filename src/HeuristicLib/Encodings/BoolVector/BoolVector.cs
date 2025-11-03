namespace HEAL.HeuristicLib.Encodings.BoolVector;

public class BoolVector : IReadOnlyList<bool> {
  private readonly bool[] elements;
  
  public BoolVector(IEnumerable<bool> elements) {
    this.elements = elements.ToArray();
  }
  
  public BoolVector(bool value) {
    elements = [value];
  }
  
  public static implicit operator BoolVector(bool value) => new BoolVector(value);
  
  public bool this[int index] => elements[index];
  
  public bool this[Index index] => elements[index];
  
  public IEnumerator<bool> GetEnumerator() => ((IEnumerable<bool>)elements).GetEnumerator();
  
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => elements.GetEnumerator();
  
  public int Count => elements.Length;
  
  public bool Contains(bool value) => elements.Contains(value);
  
  public static bool AreCompatible(BoolVector a, BoolVector b) {
    return a.Count == b.Count || a.Count == 1 || b.Count == 1;
  }
  
  public static int BroadcastLength(BoolVector a, BoolVector b) {
    return Math.Max(a.Count, b.Count);
  }
  
  // Boolean operations
  public static BoolVector And(BoolVector a, BoolVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for logical operations");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      bool aValue = a.Count == 1 ? a[0] : a[i];
      bool bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue && bValue;
    }
    
    return new BoolVector(result);
  }
  
  public static BoolVector Or(BoolVector a, BoolVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for logical operations");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      bool aValue = a.Count == 1 ? a[0] : a[i];
      bool bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue || bValue;
    }
    
    return new BoolVector(result);
  }
  
  public static BoolVector Xor(BoolVector a, BoolVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for logical operations");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      bool aValue = a.Count == 1 ? a[0] : a[i];
      bool bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue ^ bValue;
    }
    
    return new BoolVector(result);
  }
  
  public static BoolVector Not(BoolVector a) {
    bool[] result = new bool[a.Count];
    
    for (int i = 0; i < a.Count; i++) {
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
  public bool All() {
    foreach (bool element in elements) {
      if (!element) return false;
    }
    return true;
  }
  
  public bool Any() {
    foreach (bool element in elements) {
      if (element) return true;
    }
    return false;
  }
  
  public int TrueCount() {
    int count = 0;
    foreach (bool element in elements) {
      if (element) count++;
    }
    return count;
  }
  
  public override string ToString() {
    return $"[{string.Join(", ", elements.Select(b => b ? "True" : "False"))}]";
  }
}
