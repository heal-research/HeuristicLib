using System.Text.RegularExpressions;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public partial class NaturalStringComparer : IComparer<string> {
  public int Compare(string? x, string? y) {
    if (x == y)
      return 0;
    if (x == null)
      return y == null ? 0 : -1;
    if (y == null)
      return 1;

    string[] first = SplitByNumbersRegex().Split(x);
    string[] second = SplitByNumbersRegex().Split(y);

    for (int i = 0; i < first.Length && i < second.Length; i++) {
      if (first[i] != second[i])
        return CompareWithParsing(first[i], second[i]);
    }

    if (first.Length < second.Length)
      return -1;
    return second.Length < first.Length ? 1 : 0;
  }

  private static int CompareWithParsing(string x, string y) {
    if (int.TryParse(x, out var first) && int.TryParse(y, out var second))
      return first.CompareTo(second);
    return string.Compare(x, y, StringComparison.Ordinal);
  }

  [GeneratedRegex("([0-9]+)")]
  private static partial Regex SplitByNumbersRegex();
}
