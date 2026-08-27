namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public static class QaplibQapInstanceProvider
{
    public const string FileExtension = "dat";

    private static readonly HashSet<string> ReversedSolutions = new(StringComparer.OrdinalIgnoreCase)
    {
        "bur26a",
        "bur26b",
        "bur26c",
        "bur26d",
        "bur26e",
        "bur26f",
        "bur26g",
        "bur26h",
        "chr12a",
        "chr12b",
        "chr12c",
        "chr15a",
        "chr15b",
        "chr15c",
        "chr18a",
        "chr18b",
        "chr20a",
        "chr20b",
        "chr20c",
        "chr22a",
        "chr22b",
        "chr25a",
        "esc16a",
        "esc16b",
        "esc16c",
        "esc16d",
        "esc16e",
        "esc16g",
        "esc16h",
        "esc16i",
        "esc16j",
        "esc32a",
        "esc32b",
        "esc32c",
        "esc32d",
        "esc32e",
        "esc32f",
        "esc32g",
        "esc32h",
        "had12",
        "had14",
        "had16",
        "had18",
        "had20",
        "kra32",
        "lipa20a",
        "lipa30a",
        "lipa40a",
        "lipa50a",
        "lipa60a",
        "lipa70a",
        "lipa80a",
        "lipa90a",
        "nug12",
        "nug14",
        "nug15",
        "nug16a",
        "nug16b",
        "nug17",
        "nug18",
        "nug20",
        "nug21",
        "nug22",
        "nug24",
        "nug25",
        "nug27",
        "nug28",
        "rou12",
        "rou15",
        "rou20",
        "scr12",
        "scr15",
        "scr20",
        "sko100a",
        "sko100b",
        "sko100c",
        "sko100d",
        "sko100e",
        "sko100f",
        "sko49",
        "sko81",
        "sko90",
        "ste36a",
        "ste36b",
        "tai100a",
        "tai100b",
        "tai12a",
        "tai12b",
        "tai150b",
        "tai15a",
        "tai15b",
        "tai17a",
        "tai20a",
        "tai20b",
        "tai256c",
        "tai25a",
        "tai25b",
        "tai30a",
        "tai30b",
        "tai35a",
        "tai35b",
        "tai40a",
        "tai40b",
        "tai50a",
        "tai50b",
        "tai60a",
        "tai60b",
        "tai64c",
        "tai80a",
        "tai80b",
        "wil100"
    };

    public static QapData LoadData(string qapFile, string? solutionFile = null)
    {
        var parser = new QaplibParser(qapFile);
        parser.Parse();

        var data = new QapData
        {
            Name = Path.GetFileNameWithoutExtension(qapFile),
            Description = "Loaded from file \"" + qapFile + "\".",
            Dimension = parser.Size,
            Distances = parser.Distances,
            Flows = parser.Flows
        };

        if (!string.IsNullOrEmpty(solutionFile))
        {
            var solutionParser = new QaplibSolutionParser(solutionFile);
            solutionParser.Parse(valueAsLocation: true);

            data.BestKnownAssignment = ReversedSolutions.Contains(data.Name)
              ? ReverseAssignment(solutionParser.Assignment)
              : solutionParser.Assignment;
            data.BestKnownQuality = solutionParser.Quality;
        }

        return data;
    }

    private static int[] ReverseAssignment(int[] assignment)
    {
        var reversed = new int[assignment.Length];
        for (var i = 0; i < assignment.Length; i++)
        {
            reversed[assignment[i]] = i;
        }

        return reversed;
    }
}
