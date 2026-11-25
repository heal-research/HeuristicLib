using HEAL.HeuristicLib.Tests.Algorithms.Fixtures;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class OperatorCutoverTests
{
    // Baselines were captured from f576e14a and cover every candidate and quality in each retained generation.
    [Theory]
    [InlineData("recommendedDefaults", "04D7E22EF37D96307D1AEE7B2BE2B5C3119D7B674C81B3BEC1C49FACE917A38C")]
    [InlineData("defaults", "C8C873343A3E35A0F47B7A304AAC7AFF14E02359C8F521B14A95C9CBE6DF8327")]
    [InlineData("seed7", "F1B2A305E41AC7E934D79DBB877F42FC726304BE6084FC01D8A5910F27946028")]
    [InlineData("seed42", "CD58404AEA99A38177229F9E0B46D3C21186A02DD69370CA689AB06EC0D3FAAB")]
    [InlineData("rate0", "34A209B93475B56E1CF0412AA815E4370092062775F9B2AEC111611381AA004B")]
    [InlineData("rate1", "B650A2C074A5AD9AB31F0F6D15A16C9655A05CB1327F5902705F41245B563534")]
    [InlineData("rateNaN", "34A209B93475B56E1CF0412AA815E4370092062775F9B2AEC111611381AA004B")]
    [InlineData("zeroLimit", "4F53CDA18C2BAA0C0354BB5F9A3ECBE5ED12AB4D8E11BA873C2F11161202B945")]
    [InlineData("negativeLimit", "4F53CDA18C2BAA0C0354BB5F9A3ECBE5ED12AB4D8E11BA873C2F11161202B945")]
    [InlineData("unlimited", "EFA193040D35A5467EB19D2802A954CD7476EF736532D891A34AEF7291D03D7F")]
    public async Task GeneticAlgorithm_PreservesEveryCapturedGeneration(string scenario, string baseline) =>
        (await OperatorCutoverScenarios.CaptureAsync(scenario)).ShouldBe(baseline);
}
