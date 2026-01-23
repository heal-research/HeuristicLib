using System.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public class TableFileParser : Progress<long>
{
  // reports the number of bytes read
  private const int BufferSize = 65536;

  // char used to symbolize whitespaces (no missing values can be handled with whitespaces)
  private const char WhiteSpaceChar = (char)0;
  private static readonly char[] PossibleSeparators = [',', ';', '\t', WhiteSpaceChar];
  private Tokenizer tokenizer = null!;
  private int estimatedNumberOfLines = 200; // initial capacity for columns, will be set automatically when data is read from a file

  public Encoding Encoding { get; set; } = Encoding.Default;

  public int Rows { get; set; }

  public int Columns { get; set; }

  public List<IList> Values { get; private set; } = null!;

  private List<string> variableNames = [];

  public IEnumerable<string> VariableNames
  {
    get {
      if (variableNames.Count > 0) {
        return variableNames;
      }

      var names = new string[Columns];
      for (var i = 0; i < names.Length; i++) {
        names[i] = "X" + i.ToString("000");
      }

      return names;
    }
  }

  public bool AreColumnNamesInFirstLine(string fileName)
  {
    DetermineFileFormat(fileName, out var numberFormat, out var dateTimeFormatInfo, out var separator);
    using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    return AreColumnNamesInFirstLine(stream, numberFormat, dateTimeFormatInfo, separator);
  }

  public bool AreColumnNamesInFirstLine(Stream stream)
  {
    var numberFormat = NumberFormatInfo.InvariantInfo;
    var dateTimeFormatInfo = DateTimeFormatInfo.InvariantInfo;
    var separator = ',';
    return AreColumnNamesInFirstLine(stream, numberFormat, dateTimeFormatInfo, separator);
  }

  public bool AreColumnNamesInFirstLine(string fileName, NumberFormatInfo numberFormat,
    DateTimeFormatInfo dateTimeFormatInfo, char separator)
  {
    using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    return AreColumnNamesInFirstLine(stream, numberFormat, dateTimeFormatInfo, separator);
  }

  public bool AreColumnNamesInFirstLine(Stream stream, NumberFormatInfo numberFormat,
    DateTimeFormatInfo dateTimeFormatInfo, char separator)
  {
    using var reader = new StreamReader(stream, Encoding);
    tokenizer = new Tokenizer(reader, numberFormat, dateTimeFormatInfo, separator);
    return tokenizer.PeekType() != TokenType.Double;
  }

  /// <summary>
  ///   Parses a file and determines the format first
  /// </summary>
  /// <param name="fileName">file which is parsed</param>
  /// <param name="columnNamesInFirstLine"></param>
  /// <param name="lineLimit"></param>
  public void Parse(string fileName, bool columnNamesInFirstLine, int lineLimit = -1)
  {
    DetermineFileFormat(fileName, out var numberFormat, out var dateTimeFormatInfo, out var separator);
    EstimateNumberOfLines(fileName);
    Parse(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), numberFormat, dateTimeFormatInfo, separator, columnNamesInFirstLine, lineLimit);
  }

  /// <summary>
  ///   Parses a file with the given formats
  /// </summary>
  /// <param name="fileName">file which is parsed</param>
  /// <param name="numberFormat">Format of numbers</param>
  /// <param name="dateTimeFormatInfo">Format of datetime</param>
  /// <param name="separator">defines the separator</param>
  /// <param name="columnNamesInFirstLine"></param>
  /// <param name="lineLimit"></param>
  public void Parse(string fileName, NumberFormatInfo numberFormat, DateTimeFormatInfo dateTimeFormatInfo, char separator, bool columnNamesInFirstLine, int lineLimit = -1)
  {
    EstimateNumberOfLines(fileName);
    using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    Parse(stream, numberFormat, dateTimeFormatInfo, separator, columnNamesInFirstLine, lineLimit);
  }

  /// <summary>
  ///   Takes a Stream and parses it with default format. NumberFormatInfo.InvariantInfo, DateTimeFormatInfo.InvariantInfo
  ///   and separator = ','
  /// </summary>
  /// <param name="stream">stream which is parsed</param>
  /// <param name="columnNamesInFirstLine"></param>
  /// <param name="lineLimit"></param>
  public void Parse(Stream stream, bool columnNamesInFirstLine, int lineLimit = -1)
  {
    var numberFormat = NumberFormatInfo.InvariantInfo;
    var dateTimeFormatInfo = DateTimeFormatInfo.InvariantInfo;
    var separator = ',';
    Parse(stream, numberFormat, dateTimeFormatInfo, separator, columnNamesInFirstLine, lineLimit);
  }

  /// <summary>
  ///   Parses a stream with the given formats.
  /// </summary>
  /// <param name="stream">Stream which is parsed</param>
  /// <param name="numberFormat">Format of numbers</param>
  /// <param name="dateTimeFormatInfo">Format of datetime</param>
  /// <param name="separator">defines the separator</param>
  /// <param name="columnNamesInFirstLine"></param>
  /// <param name="lineLimit"></param>
  public void Parse(Stream stream, NumberFormatInfo numberFormat, DateTimeFormatInfo dateTimeFormatInfo, char separator, bool columnNamesInFirstLine, int lineLimit = -1)
  {
    if (lineLimit > 0) {
      estimatedNumberOfLines = lineLimit;
    }

    using (var reader = new StreamReader(stream)) {
      tokenizer = new Tokenizer(reader, numberFormat, dateTimeFormatInfo, separator);
      var strValues = new List<List<string>>();
      Values = [];
      Prepare(columnNamesInFirstLine, strValues);

      var nLinesParsed = 0;
      var colIdx = 0;
      while (tokenizer.HasNext() && (lineLimit < 0 || nLinesParsed < lineLimit)) {
        if (tokenizer.PeekType() == TokenType.NewLine) {
          tokenizer.Skip();

          // all rows have to have the same number of values
          // the first row defines how many elements are needed
          if (colIdx > 0 && Values.Count != colIdx) {
            // read at least one value in the row (support for skipping empty lines)
            Error("The first row of the dataset has " + Values.Count + " columns." + Environment.NewLine +
                  "Line " + tokenizer.CurrentLineNumber + " has " + colIdx + " columns.", "",
              tokenizer.CurrentLineNumber);
          }

          OnReport(tokenizer.BytesRead);

          nLinesParsed++;
          colIdx = 0;
        } else {
          // read one value
          tokenizer.Next(out var type, out var strVal, out var dblVal, out var dateTimeVal);

          if (colIdx == Values.Count) {
            Error("The first row of the dataset has " + Values.Count + " columns." + Environment.NewLine +
                  "Line " + tokenizer.CurrentLineNumber + " has more columns.", "",
              tokenizer.CurrentLineNumber);
          }

          if (!IsColumnTypeCompatible(Values[colIdx], type)) {
            Values[colIdx] = strValues[colIdx];
          }

          // add the value to the column
          AddValue(type, Values[colIdx], strVal, dblVal, dateTimeVal);
          if (!(Values[colIdx] is List<string>)) {
            // optimization: don't store the string values in another list if the column is list<string>
            strValues[colIdx].Add(strVal);
          }

          colIdx++;
        }
      }
    }

    if (Values.Count == 0 || Values[0].Count == 0) {
      Error("Couldn't parse data values. Probably because of incorrect number format " +
            "(the parser expects english number format with a '.' as decimal separator).", "", tokenizer.CurrentLineNumber);
    }

    Rows = Values[0].Count;
    Columns = Values.Count;

    // replace lists with undefined type (object) with double-lists
    for (var i = 0; i < Values.Count; i++) {
      if (Values[i] is List<object>) {
        Values[i] = Enumerable.Repeat(double.NaN, Rows).ToList();
      }
    }

    // after everything has been parsed make sure the lists are as compact as possible
    foreach (var l in Values) {
      switch (l) {
        case List<byte> l1:
          l1.TrimExcess();
          break;
        case List<DateTime> l1:
          l1.TrimExcess();
          break;
        case List<string> l1:
          l1.TrimExcess();
          break;
        case List<object> l1:
          l1.TrimExcess();
          break;
        case List<double> l1:
          l1.TrimExcess();
          break;
      }
    }
  }

  // determines the number of newline characters in the first 64KB to guess the number of rows for a file
  private void EstimateNumberOfLines(string fileName)
  {
    var len = new FileInfo(fileName).Length;
    var buf = new char[1024 * 1024];
    using (var reader = new StreamReader(fileName, Encoding)) {
      reader.ReadBlock(buf, 0, buf.Length);
    }

    var numNewLine = 0;
    int charsInCurrentLine = 0, charsInFirstLine = 0; // the first line (names) and the last line (incomplete) are not representative
    foreach (var ch in buf) {
      charsInCurrentLine++;
      if (ch != '\n') {
        continue;
      }

      if (numNewLine == 0) {
        charsInFirstLine = charsInCurrentLine; // store the number of chars in the first line
      }

      charsInCurrentLine = 0;
      numNewLine++;
    }

    if (numNewLine <= 1) {
      // fail -> keep the default setting
    } else {
      var charsPerLineFactor = (buf.Length - charsInFirstLine - charsInCurrentLine) / ((double)numNewLine - 1);
      var estimatedLines = len / charsPerLineFactor;
      estimatedNumberOfLines = (int)Math.Round(estimatedLines * 1.1); // pessimistic allocation of 110% to make sure that the list is very likely large enough
    }
  }

  private void Prepare(bool columnNamesInFirstLine, List<List<string>> strValues)
  {
    if (columnNamesInFirstLine) {
      ParseVariableNames();
      if (!tokenizer.HasNext()) {
        Error(
          "Couldn't parse data values. Probably because of incorrect number format (the parser expects english number format with a '.' as decimal separator).",
          "", tokenizer.CurrentLineNumber);
      }
    }

    // read first line to determine types and allocate specific lists
    // read values... start in first row 
    var colIdx = 0;
    while (tokenizer.PeekType() != TokenType.NewLine) {
      // read one value
      tokenizer.Next(out var type, out var strVal, out var dblVal, out var dateTimeVal);

      // initialize column
      Values.Add(CreateList(type, estimatedNumberOfLines));
      if (type == TokenType.String) {
        strValues.Add([]); // optimization: don't store the string values in another list if the column is list<string>
      } else {
        strValues.Add(new List<string>(estimatedNumberOfLines));
      }

      AddValue(type, Values[colIdx], strVal, dblVal, dateTimeVal);
      if (type != TokenType.String) {
        strValues[colIdx].Add(strVal);
      }

      colIdx++;
    }

    tokenizer.Skip(); // skip newline
  }

  #region type-dependent dispatch

  private static bool IsColumnTypeCompatible(IList list, TokenType tokenType)
  {
    return list is List<object> || // unknown lists are compatible to everything (potential conversion)
           list is List<string> || // all tokens can be added to a string list
           tokenType == TokenType.Missing || // empty entries are allowed in all columns
           (tokenType == TokenType.Double && list is List<double>) ||
           (tokenType == TokenType.DateTime && list is List<DateTime>);
  }

  private void AddValue(TokenType type, IList list, string strVal, double dblVal, DateTime dateTimeVal)
  {
    switch (list) {
      // Add value if list has a defined type
      case List<double> dblList:
        AddValue(type, dblList, dblVal);
        return;
      case List<string> strList:
        AddValue(type, strList, strVal);
        return;
      case List<DateTime> dtList:
        AddValue(type, dtList, dateTimeVal);
        return;
    }

    // Undefined list-type 
    if (type == TokenType.Missing) {
      // add null to track number of missing values
      list.Add(null);
    } else {
      // first non-missing value for undefined list-type
      var newList = ConvertList(type, list, estimatedNumberOfLines);
      // replace list
      var idx = Values.IndexOf(list);
      Values[idx] = newList;
      // recursively call AddValue
      AddValue(type, newList, strVal, dblVal, dateTimeVal);
    }
  }

  private static void AddValue(TokenType type, List<double> list, double dblVal)
  {
    Contract.Assert(type == TokenType.Missing || type == TokenType.Double);
    list.Add(type == TokenType.Missing ? double.NaN : dblVal);
  }

  private static void AddValue(TokenType type, List<string> list, string strVal)
  {
    // assumes that strVal is always set to the original token read from the input file
    list.Add(type == TokenType.Missing ? string.Empty : strVal);
  }

  private static void AddValue(TokenType type, List<DateTime> list, DateTime dtVal)
  {
    Contract.Assert(type == TokenType.Missing || type == TokenType.DateTime);
    list.Add(type == TokenType.Missing ? DateTime.MinValue : dtVal);
  }

  private static IList CreateList(TokenType type, int estimatedNumberOfLines)
  {
    switch (type) {
      case TokenType.String:
        return new List<string>(estimatedNumberOfLines);
      case TokenType.Double:
        return new List<double>(estimatedNumberOfLines);
      case TokenType.DateTime:
        return new List<DateTime>(estimatedNumberOfLines);
      case TokenType.Missing: // List<object> represent list of unknown type
        return new List<object>(estimatedNumberOfLines);
      default:
        throw new InvalidOperationException();
    }
  }

  private static IList ConvertList(TokenType type, IList list, int estimatedNumberOfLines)
  {
    var newList = CreateList(type, estimatedNumberOfLines);
    var missingValue = GetMissingValue(type);
    for (var i = 0; i < list.Count; i++) {
      newList.Add(missingValue);
    }

    return newList;
  }

  private static object GetMissingValue(TokenType type)
  {
    switch (type) {
      case TokenType.String:
        return string.Empty;
      case TokenType.Double:
        return double.NaN;
      case TokenType.DateTime:
        return DateTime.MinValue;
      default:
        throw new ArgumentOutOfRangeException("type", type, "No missing value defined");
    }
  }

  #endregion

  public static void DetermineFileFormat(string path, out NumberFormatInfo numberFormat, out DateTimeFormatInfo dateTimeFormatInfo, out char separator) => DetermineFileFormat(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), out numberFormat, out dateTimeFormatInfo, out separator);

  public static void DetermineFileFormat(Stream stream, out NumberFormatInfo numberFormat, out DateTimeFormatInfo dateTimeFormatInfo, out char separator)
  {
    using var reader = new StreamReader(stream);
    // skip first line
    reader.ReadLine();
    // read a block
    var buffer = new char[BufferSize];
    var charsRead = reader.ReadBlock(buffer, 0, BufferSize);
    // count frequency of special characters
    var charCounts = buffer.Take(charsRead)
      .GroupBy(c => c)
      .ToDictionary(g => g.Key, g => g.Count());

    // depending on the characters occuring in the block 
    // we distinguish a number of different cases based on the following rules:
    // many points => it must be English number format, the other frequently occuring char is the separator
    // no points but many commas => this is the problematic case. Either German format (real numbers) or English format (only integer numbers) with ',' as separator
    //   => check the line in more detail:
    //            English: 0, 0, 0, 0
    //            German:  0,0 0,0 0,0 ...
    //            => if commas are followed by space => English format
    // no points no commas => English format (only integer numbers) use the other frequently occuring char as separator
    // in all cases only treat ' ' as separator if no other separator is possible (spaces can also occur additionally to separators)
    if (OccurrencesOf(charCounts, '.') > 10 || OccurrencesOf(charCounts, ',') <= 10) {
      numberFormat = NumberFormatInfo.InvariantInfo;
      dateTimeFormatInfo = DateTimeFormatInfo.InvariantInfo;
      separator = PossibleSeparators
        .Where(c => OccurrencesOf(charCounts, c) > 10)
        .OrderBy(c => -OccurrencesOf(charCounts, c))
        .DefaultIfEmpty(' ')
        .First();
    } else {
      // no points and many commas
      // count the number of tokens (chains of only digits and commas) that contain multiple comma characters
      var tokensWithMultipleCommas = 0;
      for (var i = 0; i < charsRead; i++) {
        var nCommas = 0;
        while (i < charsRead && (buffer[i] == ',' || char.IsDigit(buffer[i]))) {
          if (buffer[i] == ',') {
            nCommas++;
          }

          i++;
        }

        if (nCommas > 2) {
          tokensWithMultipleCommas++;
        }
      }

      if (tokensWithMultipleCommas > 1) {
        // English format (only integer values) with ',' as separator
        numberFormat = NumberFormatInfo.InvariantInfo;
        dateTimeFormatInfo = DateTimeFormatInfo.InvariantInfo;
        separator = ',';
      } else {
        char[] disallowedSeparators = [',']; // n. def. contains a space so ' ' should be disallowed to, however existing unit tests would fail
        // German format (real values)
        numberFormat = NumberFormatInfo.GetInstance(new CultureInfo("de-DE"));
        dateTimeFormatInfo = DateTimeFormatInfo.GetInstance(new CultureInfo("de-DE"));
        separator = PossibleSeparators
          .Except(disallowedSeparators)
          .Where(c => OccurrencesOf(charCounts, c) > 10)
          .OrderBy(c => -OccurrencesOf(charCounts, c))
          .DefaultIfEmpty(' ')
          .First();
      }
    }
  }

  private static int OccurrencesOf(Dictionary<char, int> charCounts, char c) => charCounts.GetValueOrDefault(c, 0);

  #region tokenizer

  // the tokenizer reads full lines and returns separated tokens in the line as well as a terminating end-of-line character
  internal enum TokenType
  {
    NewLine = 0, String = 1, Double = 2, DateTime = 3, Missing = 4
  }

  internal class Tokenizer
  {
    private readonly StreamReader reader;

    // we assume that a buffer of 1024 tokens for a line is sufficient most of the time (the buffer is increased below if necessary)
    private TokenType[] tokenTypes = new TokenType[1024];
    private string[] stringVals = new string[1024];
    private double[] doubleVals = new double[1024];
    private DateTime[] dateTimeVals = new DateTime[1024];
    private int tokenPos;
    private int numTokens;
    private readonly NumberFormatInfo numberFormatInfo;
    private readonly DateTimeFormatInfo dateTimeFormatInfo;
    private readonly char separator;

    // arrays for string.Split()
    private readonly char[] whiteSpaceSeparators = []; // string split uses separators as default
    private readonly char[] separators;

    public int CurrentLineNumber { get; private set; }
    public string CurrentLine { get; private set; } = string.Empty;

    public long BytesRead
    {
      get;
      private set;
    }

    public Tokenizer(StreamReader reader, NumberFormatInfo numberFormatInfo, DateTimeFormatInfo dateTimeFormatInfo, char separator)
    {
      this.reader = reader;
      this.numberFormatInfo = numberFormatInfo;
      this.dateTimeFormatInfo = dateTimeFormatInfo;
      this.separator = separator;
      separators = [separator];
      ReadNextTokens();
    }

    public bool HasNext() => numTokens > tokenPos || !reader.EndOfStream;

    public TokenType PeekType() => tokenTypes[tokenPos];

    public void Skip()
    {
      // simply skips one token without returning the result values
      tokenPos++;
      if (numTokens == tokenPos) {
        ReadNextTokens();
      }
    }

    public void Next(out TokenType type, out string strVal, out double dblVal, out DateTime dateTimeVal)
    {
      type = tokenTypes[tokenPos];
      strVal = stringVals[tokenPos];
      dblVal = doubleVals[tokenPos];
      dateTimeVal = dateTimeVals[tokenPos];
      Skip();
    }

    private void ReadNextTokens()
    {
      if (reader.EndOfStream) {
        return;
      }

      CurrentLine = reader.ReadLine() ?? string.Empty;
      CurrentLineNumber++;
      if (reader.BaseStream.CanSeek) {
        BytesRead = reader.BaseStream.Position;
      } else {
        BytesRead += CurrentLine.Length + 2; // guess
      }

      var i = 0;
      if (!string.IsNullOrWhiteSpace(CurrentLine)) {
        foreach (var tok in Split(CurrentLine)) {
          var type = TokenType.String; // default
          stringVals[i] = tok.Trim();
          if (double.TryParse(tok, NumberStyles.Float, numberFormatInfo, out var doubleVal)) {
            type = TokenType.Double;
            doubleVals[i] = doubleVal;
          } else if (DateTime.TryParse(tok, dateTimeFormatInfo, DateTimeStyles.NoCurrentDateDefault, out var dateTimeValue)
                     && (dateTimeValue.Year > 1 || dateTimeValue.Month > 1 || dateTimeValue.Day > 1) // if no date is given it is returned as 1.1.0001 -> don't allow this
                    ) {
            type = TokenType.DateTime;
            dateTimeVals[i] = dateTimeValue;
          } else if (string.IsNullOrWhiteSpace(tok)) {
            type = TokenType.Missing;
          }

          // couldn't parse the token as an int or float number or datetime value so return a string token

          tokenTypes[i] = type;
          i++;

          if (i >= tokenTypes.Length) {
            // increase buffer size if necessary
            IncreaseCapacity(ref tokenTypes);
            IncreaseCapacity(ref doubleVals);
            IncreaseCapacity(ref stringVals);
            IncreaseCapacity(ref dateTimeVals);
          }
        }
      }

      tokenTypes[i] = TokenType.NewLine;
      numTokens = i + 1;
      tokenPos = 0;
    }

    private IEnumerable<string> Split(string line) => separator == WhiteSpaceChar ? line.Split(whiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries) : line.Split(separators);

    private static void IncreaseCapacity<T>(ref T[] arr)
    {
      var n = (int)Math.Floor(arr.Length * 1.7); // guess
      var arr2 = new T[n];
      Array.Copy(arr, arr2, arr.Length);
      arr = arr2;
    }
  }

  #endregion

  #region parsing

  private void ParseVariableNames()
  {
    // the first line must contain variable names
    List<string> varNames = [];

    tokenizer.Next(out var type, out var strVal, out _, out _);

    // the first token must be a variable name
    if (type != TokenType.String) {
      throw new ArgumentException("Error: Expected " + TokenType.String + " got " + type);
    }

    varNames.Add(strVal);

    while (tokenizer.HasNext() && tokenizer.PeekType() != TokenType.NewLine) {
      tokenizer.Next(out type, out strVal, out _, out _);
      varNames.Add(strVal);
    }

    ExpectType(TokenType.NewLine);

    variableNames = varNames;
  }

  private void ExpectType(TokenType expectedToken)
  {
    if (tokenizer.PeekType() != expectedToken) {
      throw new ArgumentException("Error: Expected " + expectedToken + " got " + tokenizer.PeekType());
    }

    tokenizer.Skip();
  }

  private static void Error(string message, string token, int lineNumber) => throw new IOException($"Error while parsing. {message} (token: {token} lineNumber: {lineNumber}).");

  #endregion
}
