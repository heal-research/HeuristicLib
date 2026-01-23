using System.Collections;

#pragma warning disable S2368 //The work with 2-dimensional rectangular  arrays is explicitly wanted here 

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class Dataset
{
  protected Dictionary<string, IList> VariableValues { get; }

  protected List<string> VariableNames { get; }

  public IReadOnlyList<string> GetVariableNames() => VariableNames;

  public int Rows { get; protected set; }

  public int Columns => VariableNames.Count;

  protected Dataset()
  {
    VariableNames = [];
    VariableValues = new Dictionary<string, IList>();
    Rows = 0;
  }

  /// <summary>
  ///   Creates a new dataset. The variableValues are not cloned.
  /// </summary>
  /// <param name="variableNames">The names of the variables in the dataset</param>
  /// <param name="variableValues">The values for the variables (column-oriented storage). Values are not cloned!</param>
  protected Dataset(IEnumerable<string> variableNames, IEnumerable<IList> variableValues)
    : this(variableNames, variableValues, true)
  {
  }

  protected Dataset(IEnumerable<string> variableNames, IEnumerable<IList> variableValues, bool cloneValues)
  {
    var vNames = variableNames.ToList();
    var vValues = variableValues.ToList();
    VariableNames = vNames.Count > 0 ? vNames : Enumerable.Range(0, vValues.Count).Select(x => "Column " + x).ToList();
    // check if the arguments are consistent (no duplicate variables, same number of rows, correct data types, ...)
    CheckArguments(VariableNames, vValues);
    Rows = vValues[0].Count;
    if (cloneValues) {
      VariableValues = CloneValues(VariableNames, vValues);
    } else {
      VariableValues = new Dictionary<string, IList>(VariableNames.Count);
      for (var i = 0; i < VariableNames.Count; i++) {
        VariableValues.Add(VariableNames[i], vValues[i]);
      }
    }
  }

  protected Dataset(IEnumerable<string> variableNames, double[,] variableValues)
  {
    var vNames = variableNames.ToList();
    if (vNames.Count != variableValues.GetLength(1)) {
      throw new ArgumentException("Number of variable names doesn't match the number of columns of variableValues");
    }

    if (vNames.Distinct().Count() != vNames.Count) {
      var duplicateVariableNames = vNames.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
      var message = "The dataset cannot contain duplicate variables names: " + Environment.NewLine;
      message = duplicateVariableNames.Aggregate(message, (current, duplicateVariableName) =>
        current + duplicateVariableName + Environment.NewLine);
      throw new ArgumentException(message);
    }

    Rows = variableValues.GetLength(0);
    VariableNames = vNames;

    VariableValues = new Dictionary<string, IList>(variableValues.GetLength(1));
    for (var col = 0; col < variableValues.GetLength(1); col++) {
      var columName = VariableNames[col];
      var values = new List<double>(variableValues.GetLength(0));
      for (var row = 0; row < variableValues.GetLength(0); row++) {
        values.Add(variableValues[row, col]);
      }

      VariableValues.Add(columName, values);
    }
  }

  public ModifiableDataset ToModifiable() => new(VariableNames, VariableNames.Select(n => VariableValues[n]), true);

  public static Dataset FromRowData(IEnumerable<string> variableNames, double[,] data) => new ModifiableDataset(variableNames, data);

  public static Dataset FromRowData(IEnumerable<string> variableNames, IEnumerable<IEnumerable> data)
  {
    var vNames = variableNames.ToList();
    var transposed = new List<IList>();

    var rowCount = 0;
    foreach (var row in data) {
      var colCount = 0;
      foreach (var value in row) {
        if (colCount >= vNames.Count) {
          throw new ArgumentException("There are more variables in data, than variable names.", nameof(variableNames));
        }

        if (value == null) {
          throw new ArgumentException("Null values are not supported.", nameof(data));
        }

        if (rowCount == 0) {
          switch (value) {
            case double d:
              transposed.Add(new List<double> { d });
              break;
            case DateTime dt:
              transposed.Add(new List<DateTime> { dt });
              break;
            case string s:
              transposed.Add(new List<string> { s });
              break;
            default:
              throw new NotSupportedException($"Variable {vNames[colCount]} has type {value.GetType()}. This is not supported when converting from row-wise data.");
          }
        } else {
          transposed[colCount].Add(value);
        }

        colCount++;
      }

      if (colCount < vNames.Count) {
        throw new ArgumentException($"There are less variables in row{rowCount}, than variable names.", nameof(variableNames));
      }

      rowCount++;
    }

    if (rowCount == 0) {
      throw new ArgumentException("Data does not contain any rows", nameof(data));
    }

    return new ModifiableDataset(vNames, transposed);
  }

  public bool ContainsVariable(string variableName) => VariableValues.ContainsKey(variableName);

  public IEnumerable<string> DoubleVariables => VariableValues.Where(p => p.Value is IList<double>).Select(p => p.Key);
  public IEnumerable<string> StringVariables => VariableValues.Where(p => p.Value is IList<string>).Select(p => p.Key);
  public IEnumerable<string> DateTimeVariables => VariableValues.Where(p => p.Value is IList<DateTime>).Select(p => p.Key);

  public IReadOnlyList<double> GetDoubleValues(string variableName) => GetValues<double>(variableName);
  public IEnumerable<double> GetDoubleValues(string variableName, IEnumerable<int> rows) => GetValues<double>(variableName, rows);
  public IReadOnlyList<string> GetStringValues(string variableName) => GetValues<string>(variableName);
  public IEnumerable<string> GetStringValues(string variableName, IEnumerable<int> rows) => GetValues<string>(variableName, rows);
  public IReadOnlyList<DateTime> GetDateTimeValues(string variableName) => GetValues<DateTime>(variableName);
  public IEnumerable<DateTime> GetDateTimeValues(string variableName, IEnumerable<int> rows) => GetValues<DateTime>(variableName, rows);

  public double GetDoubleValue(string variableName, int row) => GetValues<double>(variableName)[row];
  public string GetStringValue(string variableName, int row) => GetValues<string>(variableName)[row];
  public DateTime GetDateTimeValue(string variableName, int row) => GetValues<DateTime>(variableName)[row];

  private IEnumerable<T> GetValues<T>(string variableName, IEnumerable<int> rows) => rows.Select(x => GetValues<T>(variableName)[x]);

  private List<T> GetValues<T>(string variableName)
  {
    if (!VariableValues.TryGetValue(variableName, out var list)) {
      throw new ArgumentException("The variable " + variableName + " does not exist in the dataset.");
    }

    return list switch {
      List<T> values => values,
      IList<T> => throw new NotImplementedException($"Dataset is supposed to work on List<T> directly. This is a bug. Actual type is {list.GetType()}"),
      _ => throw new ArgumentException("The variable " + variableName + " is not a " + typeof(T) + " variable.")
    };
  }

  public bool VariableHasType<T>(string variableName) => VariableValues[variableName] is IList<T>;

  protected Type GetVariableType(string variableName)
  {
    VariableValues.TryGetValue(variableName, out var list);
    if (list == null) {
      throw new ArgumentException("The variable " + variableName + " does not exist in the dataset.");
    }

    return GetElementType(list);
  }

  protected static Type GetElementType(IList list)
  {
    var type = list.GetType();
    return (type.IsGenericType ? type.GetGenericArguments()[0] : type.GetElementType())!;
  }

  protected static bool IsAllowedType(IList list)
  {
    var type = GetElementType(list);
    return IsAllowedType(type);
  }

  protected static bool IsAllowedType(Type type) => type == typeof(double) || type == typeof(string) || type == typeof(DateTime);

  protected static void CheckArguments(ICollection<string> variableNames, ICollection<IList> variableValues)
  {
    if (variableNames.Count != variableValues.Count) {
      throw new ArgumentException("Number of variable names doesn't match the number of columns of variableValues");
    }

    if (variableValues.Any(list => list.Count != variableValues.First().Count)) {
      throw new ArgumentException("The number of values must be equal for every variable");
    }

    if (variableNames.Distinct().Count() != variableNames.Count) {
      var duplicateVariableNames =
        variableNames.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
      var message = "The dataset cannot contain duplicate variables names: " + Environment.NewLine;
      message = duplicateVariableNames.Aggregate(message,
        (current, duplicateVariableName) => current + duplicateVariableName + Environment.NewLine);
      throw new ArgumentException(message);
    }

    // check if all the variables are supported
    foreach (var (variableName, values) in variableNames.Zip(variableValues, Tuple.Create)) {
      if (!IsAllowedType(values)) {
        throw new ArgumentException($"Unsupported type {GetElementType(values)} for variable {variableName}.");
      }
    }
  }

  protected static Dictionary<string, IList> CloneValues(Dictionary<string, IList> variableValues) => variableValues.ToDictionary(x => x.Key, x => CloneValues(x.Value));

  protected static Dictionary<string, IList> CloneValues(IEnumerable<string> variableNames, IEnumerable<IList> variableValues) => variableNames.Zip(variableValues, Tuple.Create).ToDictionary(x => x.Item1, x => CloneValues(x.Item2));

  protected static IList CloneValues(IList values)
  {
    return values switch {
      IList<double> doubleValues => new List<double>(doubleValues),
      IList<string> stringValues => new List<string>(stringValues),
      IList<DateTime> dateTimeValues => new List<DateTime>(dateTimeValues),
      _ => throw new ArgumentException($"Unsupported variable type {GetElementType(values)}.")
    };
  }
}
