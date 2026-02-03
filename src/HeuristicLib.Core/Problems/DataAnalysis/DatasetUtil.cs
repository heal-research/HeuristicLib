using System.Collections;
using System.Linq.Expressions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

using ValuesType = Dictionary<string, IList>;

public static class DatasetUtil {
  /// <summary>
  /// Shuffle all the lists with the same shuffling.
  /// </summary>
  /// <param name="values">The value lists to be shuffled.</param>
  /// <param name="random">The random number generator</param>
  /// <returns>A new list containing shuffled copies of the original value lists.</returns>
  public static List<IList> ShuffleLists(this List<IList> values, IRandomNumberGenerator random) {
    var count = values[0].Count;
    var indices = Enumerable.Range(0, count).Shuffle(random).ToArray();
    var shuffled = new List<IList>(values.Count);
    for (var col = 0; col < values.Count; col++) {
      switch (values[col]) {
        case IList<double>:
          shuffled.Add(new List<double>());
          break;
        case IList<DateTime>:
          shuffled.Add(new List<DateTime>());
          break;
        case IList<string>:
          shuffled.Add(new List<string>());
          break;
        default:
          throw new InvalidOperationException();
      }

      for (var i = 0; i < count; i++) {
        shuffled[col].Add(values[col][indices[i]]);
      }
    }

    return shuffled;
  }

  static DatasetUtil() {
    var dataset = Expression.Parameter(typeof(Dataset));
    var variableValues = Expression.Parameter(typeof(ValuesType));
    var valuesExpression = Expression.Field(dataset, nameof(variableValues));
    var assignExpression = Expression.Assign(valuesExpression, variableValues);

    var variableValuesSetExpression = Expression.Lambda<Action<Dataset, ValuesType>>(assignExpression, dataset, variableValues);
    variableValuesSetExpression.Compile();

    var variableValuesGetExpression = Expression.Lambda<Func<Dataset, ValuesType>>(valuesExpression, dataset);
    variableValuesGetExpression.Compile();
  }

  //public static Dictionary<string, Interval> GetVariableRanges(IDataset dataset, IEnumerable<int> rows = null) {
  //  var variableRanges = new Dictionary<string, Interval>();

  //  foreach (var variable in dataset.VariableNames) {
  //    IEnumerable<double> values;
  //    if (rows == null) values = dataset.GetDoubleValues(variable);
  //    else values = dataset.GetDoubleValues(variable, rows);

  //    var range = Interval.GetInterval(values);
  //    variableRanges.Add(variable, range);
  //  }

  //  return variableRanges;
  //}

  //private static bool GetEqualValues(this Dictionary<ValuesType, ValuesType> variableValuesMapping, ValuesType originalValues, out ValuesType matchingValues) {
  //  if (variableValuesMapping.TryGetValue(originalValues, out var value)) {
  //    matchingValues = value;
  //    return true;
  //  }

  //  matchingValues = variableValuesMapping.FirstOrDefault(kv => kv.Key == kv.Value && EqualVariableValues(originalValues, kv.Key)).Key;
  //  var result = true;
  //  if (matchingValues == null) {
  //    matchingValues = originalValues;
  //    result = false;
  //  }

  //  variableValuesMapping[originalValues] = matchingValues;
  //  return result;
  //}

  //private static bool EqualVariableValues(ValuesType values1, ValuesType values2) {
  //  //compare variable names for equality
  //  if (!values1.Keys.SequenceEqual(values2.Keys)) return false;
  //  foreach (var key in values1.Keys) {
  //    var v1 = values1[key];
  //    var v2 = values2[key];
  //    if (v1.Count != v2.Count) return false;
  //    for (var i = 0; i < v1.Count; i++) {
  //      if (!v1[i]!.Equals(v2[i])) return false;
  //    }
  //  }

  //  return true;
  //}
}
