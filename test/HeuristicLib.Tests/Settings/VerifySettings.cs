using System.Runtime.CompilerServices;
using Argon;

namespace HEAL.HeuristicLib.Tests.Settings;

public static class VerifySettings {
  [ModuleInitializer]
  public static void Initialize() {
    VerifierSettings.AddExtraSettings(serializer => serializer.TypeNameHandling = TypeNameHandling.Auto);
  }
}

public class VerifyChecksTests {
  [Fact]
  public Task Run() {
    return VerifyChecks.Run();
  }
}
