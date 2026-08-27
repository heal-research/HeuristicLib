using System.Globalization;

namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public sealed class QaplibParser
{
    private static readonly char[] ItemSeparators = [' ', '\t'];

    private readonly StreamReader source = null!;
    private int currentLineNumber;

    private QaplibParser()
    {
    }

    public QaplibParser(string path)
      : this(File.OpenRead(path))
    {
    }

    public QaplibParser(Stream stream)
    {
        source = new StreamReader(stream);
    }

    public int Size { get; private set; }

    public double[,] Distances { get; private set; } = null!;

    public double[,] Flows { get; private set; } = null!;

    public void Parse()
    {
        currentLineNumber = 0;

        try
        {
            Size = ReadSize();
            Flows = ReadMatrix("flow");
            Distances = ReadMatrix("distance");
        }
        finally
        {
            source.Close();
        }
    }

    private int ReadSize()
    {
        while (!source.EndOfStream)
        {
            var line = NextLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (int.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var size) && size > 0)
            {
                return size;
            }

            throw new InvalidDataException("Parsing the QAPLIB size in line " + currentLineNumber + " failed.");
        }

        throw new InvalidDataException("Input file does not contain a QAPLIB size.");
    }

    private double[,] ReadMatrix(string matrixName)
    {
        var result = new double[Size, Size];
        var row = 0;
        var column = 0;

        while (row < Size)
        {
            if (source.EndOfStream)
            {
                throw new InvalidDataException("Reached end of stream while reading the " + matrixName + " matrix.");
            }

            var line = NextLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = line.Split(ItemSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var value in values)
            {
                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var parsedValue))
                {
                    throw new InvalidDataException("Input file contains unreadable " + matrixName + " value (" + value + ") in line " + currentLineNumber + ".");
                }

                result[row, column++] = parsedValue;
                if (column == Size)
                {
                    column = 0;
                    row++;
                    if (row == Size)
                    {
                        break;
                    }
                }
            }
        }

        return result;
    }

    private string? NextLine()
    {
        currentLineNumber++;
        return source.ReadLine();
    }
}
