using System.Runtime.CompilerServices;
using Argon;

namespace HEAL.HeuristicLib.Tests.Settings;

public static class VerifySettings
{
  [ModuleInitializer]
  public static void Initialize()
  {
    VerifierSettings.AddExtraSettings(serializer => {
      serializer.TypeNameHandling = TypeNameHandling.Auto;
      serializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full;
      serializer.DefaultValueHandling = DefaultValueHandling.Include;
    });
  }
}

public class VerifyChecksTests
{
  [Fact]
  public Task Run() => VerifyChecks.Run();
}
