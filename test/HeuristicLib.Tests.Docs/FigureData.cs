using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HEAL.HeuristicLib.Tests.Docs;

/// <summary>
/// Writes the chart data under <c>docs/.vitepress/figure-data</c> from what the documented configuration produces.
/// </summary>
/// <remarks>
/// The documentation build reads the committed files and does not run .NET. A spec computes the data of its figure and
/// passes it here, and every run writes the file. A changed file shows up in the working tree: commit it, and the pull
/// request review judges the change. Numbers are rounded to six significant digits, which is below what a chart can show
/// and keeps the files stable across platforms.
/// </remarks>
internal static class FigureData
{
    public static void Write(string name, JsonObject data)
    {
        var path = Path.Combine(RepositoryRoot(), "docs", ".vitepress", "figure-data", name + ".json");
        var json = Serialize(data);

        // An unchanged file is left alone, so a checkout with other line endings shows no change.
        if (File.Exists(path) && File.ReadAllText(path).ReplaceLineEndings("\n") == json)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static JsonArray Numbers(IEnumerable<double> values) =>
        new([.. values.Select(value => (JsonNode?)JsonValue.Create(Round(value)))]);

    public static JsonArray Numbers(IEnumerable<int> values) =>
        new([.. values.Select(value => (JsonNode?)JsonValue.Create(value))]);

    private static double Round(double value) =>
        double.Parse(value.ToString("G6", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string Serialize(JsonObject data) =>
        data.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) + "\n";

    internal static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HEAL.HeuristicLib.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
