using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HEAL.HeuristicLib.Tests.Docs;

/// <summary>
/// Keeps the chart data under <c>docs/.vitepress/figure-data</c> equal to what the documented configuration produces.
/// </summary>
/// <remarks>
/// The documentation build reads the committed files and does not run .NET. A spec computes the data of its figure and
/// passes it here: the call fails when the committed file differs. Set <c>HEURISTICLIB_UPDATE_FIGURE_DATA=1</c> to write
/// the files instead, then review and commit them. Numbers are rounded to six significant digits, which is below what a
/// chart can show and keeps the files stable across platforms.
/// </remarks>
internal static class FigureData
{
    private const string UpdateVariable = "HEURISTICLIB_UPDATE_FIGURE_DATA";

    public static void Verify(string name, JsonObject data)
    {
        var path = Path.Combine(RepositoryRoot(), "docs", ".vitepress", "figure-data", name + ".json");
        var json = Serialize(data);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return;
        }

        File.Exists(path).ShouldBeTrue($"Figure data {name}.json is missing. Run the documentation specs with {UpdateVariable}=1 to write it.");
        var committed = File.ReadAllText(path).ReplaceLineEndings("\n");
        committed.ShouldBe(json, $"Figure data {name}.json is out of date. Run the documentation specs with {UpdateVariable}=1 and review the change.");
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
