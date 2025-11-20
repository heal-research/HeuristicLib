using System.Globalization;
using static System.String;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;

public class TsplibParser {
  public string Name { get; private set; }
  public TSPLIBTypes Type { get; private set; }
  public string Comment { get; private set; }
  public int Dimension { get; private set; }
  public double? Capacity { get; private set; }
  public TSPLIBEdgeWeightTypes EdgeWeightType { get; private set; }
  public double[,] Vertices { get; private set; } = null!;
  public double[,] DisplayVertices { get; private set; } = null!;
  public double[,]? Distances { get; private set; }
  public int[,] FixedEdges { get; private set; } = null!;
  public int[] Depots { get; private set; } = null!;
  public double[] Demands { get; private set; } = null!;
  public int[][] Tour { get; private set; } = null!;

  private const char SectionSeparator = ':';
  private static readonly char[] ItemSeparator = [' ', '\t'];

  #region Private Enums
  private enum TSPLIBSections {
    Unknown = 0,
    Eof = 1,
    Name = 2,
    Type = 3,
    Comment = 4,
    Dimension = 5,
    Capacity = 6,
    EdgeWeightType = 7,
    EdgeWeightFormat = 8,
    EdgeDataFormat = 9,
    NodeCoordType = 10,
    DisplayDataType = 11,
    NodeCoordSection = 12,
    DepotSection = 13,
    DemandSection = 14,
    EdgeDataSection = 15,
    FixedEdgesSection = 16,
    DisplayDataSection = 17,
    TourSection = 18,
    EdgeWeightSection = 19
  }

  private enum TSPLIBEdgeWeightFormats {
    Unknown = 0,
    Function = 1,
    FullMatrix = 2,
    UpperRow = 3,
    LowerRow = 4,
    UpperDiagonalRow = 5,
    LowerDiagonalRow = 6,
    UpperColumn = 7,
    //LowerColumn = 8,
    UpperDiagonalColumn = 9,
    LowerDiagonalColumn = 10
  }

  private enum TSPLIBEdgeWeightDataFormats { }

  private enum TslplibNodeCoordTypes {
    Unknown = 0,
    TwodCoords = 1,
    ThreedCoords = 2,
    NoCoords = 3
  }
  #endregion

  private readonly StreamReader source = null!;
  private int currentLineNumber;

  private TSPLIBEdgeWeightFormats edgeWeightFormat;
  private TslplibNodeCoordTypes nodeCoordType;
  private TSPLIBDisplayDataTypes displayDataType;

  private TsplibParser() {
    Name = Empty;
    Comment = Empty;
    Type = TSPLIBTypes.Unknown;
    EdgeWeightType = TSPLIBEdgeWeightTypes.Unknown;

    edgeWeightFormat = TSPLIBEdgeWeightFormats.Unknown;
    nodeCoordType = TslplibNodeCoordTypes.Unknown;
    displayDataType = TSPLIBDisplayDataTypes.Unknown;
  }

  public TsplibParser(String path)
    : this() {
    source = new StreamReader(path);
  }

  public TsplibParser(Stream stream)
    : this() {
    source = new StreamReader(stream);
  }

  /// <summary>
  /// Reads the TSPLIB file and parses the elements.
  /// </summary>
  /// <exception cref="InvalidDataException">Thrown if the file has an invalid format or contains invalid data.</exception>
  public void Parse() {
    currentLineNumber = 0;

    try {
      TSPLIBSections section;
      do {
        var line = NextLine();
        section = GetSection(line, out var value);

        switch (section) {
          case TSPLIBSections.Unknown:
            break;
          case TSPLIBSections.Name:
            ReadName(value);
            break;
          case TSPLIBSections.Type:
            ReadType(value);
            break;
          case TSPLIBSections.Comment:
            ReadComment(value);
            break;
          case TSPLIBSections.Dimension:
            ReadDimension(value);
            break;
          case TSPLIBSections.Capacity:
            ReadCapacity(value);
            break;
          case TSPLIBSections.EdgeWeightType:
            ReadEdgeWeightType(value);
            break;
          case TSPLIBSections.EdgeWeightFormat:
            ReadEdgeWeightFormat(value);
            break;
          case TSPLIBSections.EdgeDataFormat:
            ReadEdgeWeightDataFormat(value);
            break;
          case TSPLIBSections.NodeCoordType:
            ReadNodeCoordType(value);
            break;
          case TSPLIBSections.DisplayDataType:
            ReadDisplayDataType(value);
            break;
          case TSPLIBSections.NodeCoordSection:
            ReadNodeCoordsSection();
            break;
          case TSPLIBSections.DepotSection:
            ReadDepotSection();
            break;
          case TSPLIBSections.DemandSection:
            ReadDemandSection();
            break;
          case TSPLIBSections.EdgeDataSection:
            ReadEdgeDataSection();
            break;
          case TSPLIBSections.FixedEdgesSection:
            ReadFixedEdgesSection();
            break;
          case TSPLIBSections.DisplayDataSection:
            ReadDisplayDataSection();
            break;
          case TSPLIBSections.TourSection:
            ReadTourSection();
            break;
          case TSPLIBSections.EdgeWeightSection:
            ReadEdgeWeightSection();
            break;
          case TSPLIBSections.Eof:
            break;
          default:
            throw new InvalidDataException("Input file contains unknown or unsupported section (" + line + ") in line " + currentLineNumber.ToString());
        }
      } while (!(section == TSPLIBSections.Eof || source.EndOfStream));
    }
    finally {
      source.Close();
    }
  }

  private string? NextLine() {
    currentLineNumber++;
    return source.ReadLine();
  }

  private static TSPLIBSections GetSection(string? line, out string value) {
    value = Empty;

    if (line == null)
      return TSPLIBSections.Unknown;

    var sectionNameEnd = line.IndexOf(SectionSeparator);
    if (sectionNameEnd == -1) sectionNameEnd = line.Length;

    var sectionName = line[..sectionNameEnd].Trim();
    if (sectionNameEnd + 1 < line.Length)
      value = line[(sectionNameEnd + 1)..];

    return Enum.TryParse(sectionName, out TSPLIBSections section) ? section : TSPLIBSections.Unknown;
  }

  private void ReadName(string value) {
    Name += value.Trim();
  }

  private void ReadType(string value) {
    Type = Enum.TryParse(value.Trim().ToUpper(), out TSPLIBTypes t) ? t : TSPLIBTypes.Unknown;
  }

  private void ReadComment(string value) {
    Comment += value.Trim() + Environment.NewLine;
  }

  private void ReadCapacity(string value) {
    if (double.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var c))
      Capacity = c;
    else throw new InvalidDataException("Parsing the capacity in line " + currentLineNumber + " failed. It is not recognized as double value.");
  }

  private void ReadDimension(string value) {
    if (int.TryParse(value.Trim(), out var dimension))
      Dimension = dimension;
    else throw new InvalidDataException("Parsing the dimension in line " + currentLineNumber + " failed. It is not recognized as an integer number.");
  }

  private void ReadEdgeWeightType(string value) {
    if (Enum.TryParse(value.Trim().ToUpper(), out TSPLIBEdgeWeightTypes e))
      EdgeWeightType = e;
    else throw new InvalidDataException("Input file contains an unsupported edge weight type (" + value + ") in line " + currentLineNumber + ".");
  }

  private void ReadEdgeWeightFormat(string value) {
    if (Enum.TryParse(value.Trim().ToUpper(), out TSPLIBEdgeWeightFormats e))
      edgeWeightFormat = e;
    else throw new InvalidDataException("Input file contains an unsupported edge weight format (" + value + ") in line " + currentLineNumber + ".");
  }

  private void ReadEdgeWeightDataFormat(string value) {
    if (!Enum.TryParse(value.Trim().ToUpper(), out TSPLIBEdgeWeightDataFormats _))
      throw new InvalidDataException("Input file contains an unsupported edge weight data format (" + value + ") in line " + currentLineNumber + ".");
  }

  private void ReadNodeCoordType(string value) {
    if (Enum.TryParse(value.Trim().ToUpper(), out TslplibNodeCoordTypes n))
      nodeCoordType = n;
    else throw new InvalidDataException("Input file contains an unsupported node coordinates type (" + value + ") in line " + currentLineNumber + ".");
  }

  private void ReadDisplayDataType(string value) {
    if (Enum.TryParse(value.Trim().ToUpper(), out TSPLIBDisplayDataTypes d))
      displayDataType = d;
    else throw new InvalidDataException("Input file contains an unsupported display data type (" + value + ") in line " + currentLineNumber + ".");
  }

  private void ReadNodeCoordsSection() {
    if (Dimension == 0)
      throw new InvalidDataException("Input file does not contain dimension information.");
    switch (nodeCoordType) {
      case TslplibNodeCoordTypes.NoCoords:
        return;
      case TslplibNodeCoordTypes.Unknown: // It's a pity that there is a documented standard which is ignored in most files.
      case TslplibNodeCoordTypes.TwodCoords:
        Vertices = new double[Dimension, 2];
        break;
      case TslplibNodeCoordTypes.ThreedCoords:
        Vertices = new double[Dimension, 3];
        break;
      default:
        throw new InvalidDataException("Input files does not specify a valid node coord type.");
    }

    for (var i = 0; i < Dimension; i++) {
      var line = NextLine();
      var tokens = line!.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);

      if (tokens.Length != Vertices.GetLength(1) + 1)
        throw new InvalidDataException("Input file contains invalid number of node coordinates in line " + currentLineNumber + ".");

      if (int.TryParse(tokens[0], out var node)) {
        for (var j = 0; j < Vertices.GetLength(1); j++)
          Vertices[node - 1, j] = double.Parse(tokens[j + 1], CultureInfo.InvariantCulture.NumberFormat);
      } else throw new InvalidDataException("Input file does not specify a valid node in line " + currentLineNumber + ".");
    }
  }

  private void ReadDepotSection() {
    var depots = new List<int>();
    do {
      var line = NextLine();
      if (!int.TryParse(line, out var node))
        throw new InvalidDataException("Input file contains an unknown depot entry at line " + currentLineNumber + ".");
      if (node == -1) break;
      depots.Add(node);
    } while (true);

    Depots = depots.ToArray();
  }

  private void ReadDemandSection() {
    if (Dimension == 0)
      throw new InvalidDataException("Input file does not contain dimension information.");
    Demands = new double[Dimension];
    for (var i = 0; i < Dimension; i++) {
      var tokens = NextLine()!.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
      if (int.TryParse(tokens[0], out var node)) {
        if (double.TryParse(tokens[1], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var demand))
          Demands[node - 1] = demand;
        else throw new InvalidDataException("Input file contains invalid demand information in line " + currentLineNumber + ".");
      } else throw new InvalidDataException("Input file contains invalid node information in line " + currentLineNumber + ".");
    }
  }

  private static void ReadEdgeDataSection() {
    throw new NotSupportedException("Files with an edge data section are not supported.");
  }

  private void ReadFixedEdgesSection() {
    var edges = new List<Tuple<int, int>>();
    var finished = false;
    while (!finished) {
      var line = NextLine();
      var tokens = line!.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
      if (tokens.Length == 1) {
        if (!int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var number))
          throw new InvalidDataException("Input file does not end the fixed edges section with \"-1\" in line " + currentLineNumber + ".");
        if (number != -1)
          throw new InvalidDataException("Input file must end the fixed edges section with a -1 in line " + currentLineNumber + ".");
        finished = true;
      } else {
        if (tokens.Length != 2)
          throw new InvalidDataException("Input file contains an error in line " + currentLineNumber + ", exactly two nodes need to be given in each line.");
        var node1 = int.Parse(tokens[0], CultureInfo.InvariantCulture.NumberFormat) - 1;
        var node2 = int.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat) - 1;
        edges.Add(Tuple.Create(node1, node2));
      }
    }

    FixedEdges = new int[edges.Count, 2];
    for (var i = 0; i < edges.Count; i++) {
      FixedEdges[i, 0] = edges[i].Item1;
      FixedEdges[i, 1] = edges[i].Item2;
    }
  }

  private void ReadDisplayDataSection() {
    if (Dimension == 0)
      throw new InvalidDataException("Input file does not contain dimension information.");

    switch (displayDataType) {
      case TSPLIBDisplayDataTypes.NoDisplay:
      case TSPLIBDisplayDataTypes.CoordinatesDisplay:
        return;
      case TSPLIBDisplayDataTypes.TwoDimensionalDisplay:
        DisplayVertices = new double[Dimension, 2];
        break;
      case TSPLIBDisplayDataTypes.Unknown:
      default:
        throw new InvalidDataException("Input files does not specify a valid display data type.");
    }

    for (var i = 0; i < Dimension; i++) {
      var line = NextLine();
      var tokens = line!.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);

      if (tokens.Length != DisplayVertices.GetLength(1) + 1)
        throw new InvalidDataException("Input file contains invalid number of display data coordinates in line " + currentLineNumber + ".");

      if (int.TryParse(tokens[0], out var node)) {
        for (var j = 0; j < DisplayVertices.GetLength(1); j++)
          DisplayVertices[node - 1, j] = double.Parse(tokens[j + 1], CultureInfo.InvariantCulture.NumberFormat);
      } else throw new InvalidDataException("Input file does not specify a valid node in line " + currentLineNumber + ".");
    }
  }

  /// <summary>
  /// Parses the tour section.
  /// </summary>
  /// <remarks>
  /// Unfortunately, none of the given files for the TSP follow the description
  /// in the TSPLIB documentation.
  /// The faulty files use only one -1 to terminate the section as well as the tour
  /// whereas the documentation says that one -1 terminates the tour and another -1
  /// terminates the section.
  /// So the parser peeks at the next character after a -1. If the next character
  /// is an E as in EOF or the stream ends, the parser ends. Otherwise it will
  /// continue to read tours until a -1 is followed by a -1.
  /// </remarks>
  private void ReadTourSection() {
    if (Dimension == 0)
      throw new InvalidDataException("Input file does not contain dimension information.");

    var tours = new List<List<int>>();
    do {
      var line = NextLine();
      if (IsNullOrEmpty(line)) break;
      var nodes = line.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
      if (nodes.Length == 0) break;

      var finished = false;
      foreach (var nodeString in nodes) {
        var node = int.Parse(nodeString, CultureInfo.InvariantCulture.NumberFormat);
        if (node == -1) {
          finished = (tours.Count > 0 && tours[^1].Count == 0) // -1 followed by -1
                     || (source.BaseStream.CanSeek && source.Peek() == -1)
                     || source.Peek() == 'E';
          if (finished) break;
          tours.Add([]);
        } else {
          if (tours.Count == 0) tours.Add([]);
          tours[^1].Add(node - 1);
        }
      }

      if (finished) break;
    } while (true);

    if (tours[^1].Count == 0) tours.RemoveAt(tours.Count - 1);
    Tour = tours.Select(x => x.ToArray()).ToArray();
  }

  private void ReadEdgeWeightSection() {
    if (Dimension == 0)
      throw new InvalidDataException("Input file does not contain dimension information.");

    switch (edgeWeightFormat) {
      case TSPLIBEdgeWeightFormats.Unknown:
        throw new InvalidDataException("Input file does not specify an edge weight format.");
      case TSPLIBEdgeWeightFormats.Function:
        return;
    }

    Distances = new double[Dimension, Dimension];

    var triangular = edgeWeightFormat != TSPLIBEdgeWeightFormats.FullMatrix;
    var upperTriangular = edgeWeightFormat is TSPLIBEdgeWeightFormats.UpperColumn or TSPLIBEdgeWeightFormats.UpperDiagonalColumn or TSPLIBEdgeWeightFormats.UpperDiagonalRow or TSPLIBEdgeWeightFormats.UpperRow;
    var diagonal = edgeWeightFormat is TSPLIBEdgeWeightFormats.LowerDiagonalColumn or TSPLIBEdgeWeightFormats.LowerDiagonalRow or TSPLIBEdgeWeightFormats.UpperDiagonalColumn or TSPLIBEdgeWeightFormats.UpperDiagonalRow;
    var rowWise = edgeWeightFormat is TSPLIBEdgeWeightFormats.LowerDiagonalRow or TSPLIBEdgeWeightFormats.LowerRow or TSPLIBEdgeWeightFormats.UpperDiagonalRow or TSPLIBEdgeWeightFormats.UpperRow;

    var diagonalInt = diagonal ? 0 : 1;
    var dim1 = triangular && !upperTriangular ? diagonalInt : 0;
    var dim2 = triangular && upperTriangular ? diagonalInt : 0;
    var finished = false;
    while (!finished) {
      var line = NextLine();
      var weights = line!.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
      foreach (var weightString in weights) {
        if (!double.TryParse(weightString, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var weight))
          throw new InvalidDataException("Input file contains unreadable weight information (" + weightString + ") in line " + currentLineNumber + ".");

        Distances[dim1, dim2] = weight;
        if (triangular) {
          Distances[dim2, dim1] = weight;
          if (upperTriangular) {
            if (rowWise) {
              dim2++;
              if (dim2 != Dimension)
                continue;

              dim1++;
              if (diagonal) dim2 = dim1;
              else dim2 = dim1 + 1;
            } else { // column-wise
              dim1++;
              if ((!diagonal || dim1 != dim2 + 1) && (diagonal || dim1 != dim2))
                continue;

              dim2++;
              dim1 = 0;
            }
          } else { // lower-triangular
            if (rowWise) {
              dim2++;
              if ((!diagonal || dim2 != dim1 + 1)
                  && (diagonal || dim2 != dim1))
                continue;

              dim1++;
              dim2 = 0;
            } else { // column-wise
              dim1++;
              if (dim1 != Dimension)
                continue;

              dim2++;
              if (diagonal) dim1 = dim2;
              else dim1 = dim2 + 1;
            }
          }
        } else { // full-matrix
          dim2++;
          if (dim2 != Dimension)
            continue;

          dim1++;
          dim2 = 0;
        }
      }

      finished = dim1 == Dimension || dim2 == Dimension;
    }
  }
}
