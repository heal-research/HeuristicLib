using System.Globalization;

namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public sealed class QaplibSolutionParser
{
    private static readonly char[] ItemSeparators = [' ', '\t', ','];

    private readonly StreamReader source = null!;
    private int currentLineNumber;

    private QaplibSolutionParser()
    {
    }

    public QaplibSolutionParser(string path)
      : this(File.OpenRead(path))
    {
    }

    public QaplibSolutionParser(Stream stream)
    {
        source = new StreamReader(stream);
    }

    public int Size { get; private set; }

    public int[] Assignment { get; private set; } = null!;

    public double Quality { get; private set; }

    public void Parse(bool valueAsLocation)
    {
        currentLineNumber = 0;

        try
        {
            ReadHeader();
            Assignment = new int[Size];
            ReadAssignment(valueAsLocation);
        }
        finally
        {
            source.Close();
        }
    }

    private void ReadHeader()
    {
        while (!source.EndOfStream)
        {
            var line = NextLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = Split(line);
            if (values.Length < 2
              || !int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var size)
              || !double.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var quality)
              || size <= 0)
            {
                throw new InvalidDataException("Parsing the QAPLIB solution header in line " + currentLineNumber + " failed.");
            }

            Size = size;
            Quality = quality;
            return;
        }

        throw new InvalidDataException("Input file does not contain a QAPLIB solution header.");
    }

    private void ReadAssignment(bool valueAsLocation)
    {
        var values = new List<int>();
        var zeroBased = false;

        while (!source.EndOfStream)
        {
            var line = NextLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            foreach (var value in Split(line))
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var parsedValue))
                {
                    throw new InvalidDataException("Input file contains unreadable assignment value (" + value + ") in line " + currentLineNumber + ".");
                }

                zeroBased |= parsedValue == 0;
                values.Add(parsedValue);
            }
        }

        if (values.Count < Size)
        {
            throw new InvalidDataException("QAPLIB solution contains fewer assignment values than expected.");
        }

        var offset = zeroBased ? 0 : 1;
        for (var i = 0; i < Size; i++)
        {
            var assignmentValue = values[i] - offset;
            if (assignmentValue < 0 || assignmentValue >= Size)
            {
                throw new InvalidDataException("QAPLIB solution contains assignment value outside the expected range.");
            }

            if (valueAsLocation)
            {
                Assignment[assignmentValue] = i;
            }
            else
            {
                Assignment[i] = assignmentValue;
            }
        }
    }

    private string? NextLine()
    {
        currentLineNumber++;
        return source.ReadLine();
    }

    private static string[] Split(string line) => line.Split(ItemSeparators, StringSplitOptions.RemoveEmptyEntries);
}
