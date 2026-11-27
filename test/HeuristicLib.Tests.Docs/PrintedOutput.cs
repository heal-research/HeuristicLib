// Console output of a test goes to its own test output, so a spec can assert what a documented program prints.
[assembly: CaptureConsole]

namespace HEAL.HeuristicLib.Tests.Docs;

/// <summary>
/// Reads what the current test wrote to the console, for specs that pin the output a page quotes.
/// </summary>
internal static class PrintedOutput
{
    public static string[] Lines()
    {
        var output = TestContext.Current.TestOutputHelper?.Output ?? "";
        return output.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
    }
}
