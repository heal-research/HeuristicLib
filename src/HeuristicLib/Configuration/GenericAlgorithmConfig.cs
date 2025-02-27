using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HEAL.HeuristicLib.Configuration;

public record GeneticAlgorithmConfig(
  int PopulationSize,
  int MaximumGenerations,
  CreatorConfig Creator,
  CrossoverConfig Crossover,
  MutatorConfig Mutator,
  double MutationRate,
  SelectorConfig Selector,
  ReplacerConfig Replacer
);

public abstract record CreatorConfig();
public record RandomPermutationCreatorConfig : CreatorConfig;
public record UniformRealVectorCreatorConfig(double[] Minimum, double[] Maximum) : CreatorConfig;
public record NormalRealVectorCreatorConfig(double[] Mean, double[] StandardDeviation, double[] Minimum, double[] Maximum) : CreatorConfig;

public abstract record CrossoverConfig();
public record OrderCrossoverConfig : CrossoverConfig;
public record SinglePointRealVectorCrossoverConfig : CrossoverConfig;
public record AlphaBlendRealVectorCrossoverConfig(double Alpha = 0.7, double Beta = 0.3) : CrossoverConfig;

public abstract record MutatorConfig();
public record SwapMutatorConfig : MutatorConfig;
public record GaussianRealVectorMutatorConfig(double[] Mean, double[] StandardDeviation) : MutatorConfig;

public abstract record SelectorConfig();
public record TournamentSelectorConfig(int TournamentSize) : SelectorConfig;
public record RouletteWheelSelectorConfig : SelectorConfig;

public abstract record ReplacerConfig();
public record ElitistReplacerConfig : ReplacerConfig;


public class OperatorConverter<T> : JsonConverter<T> where T : class
{
    private static readonly Dictionary<string, Type> TypeMap = new()
    {
        { "RandomPermutationCreator", typeof(RandomPermutationCreatorConfig) },
        { "UniformRealVectorCreator", typeof(UniformRealVectorCreatorConfig) },
        { "NormalRealVectorCreator", typeof(NormalRealVectorCreatorConfig) },
        { "OrderCrossover", typeof(OrderCrossoverConfig) },
        { "SinglePointRealVectorCrossover", typeof(SinglePointRealVectorCrossoverConfig) },
        { "AlphaBlendRealVectorCrossover", typeof(AlphaBlendRealVectorCrossoverConfig) },
        { "SwapMutator", typeof(SwapMutatorConfig) },
        { "GaussianRealVectorMutator", typeof(GaussianRealVectorMutatorConfig) },
        { "TournamentSelector", typeof(TournamentSelectorConfig) },
        { "RouletteWheelSelector", typeof(RouletteWheelSelectorConfig) },
        { "ElitistReplacer", typeof(ElitistReplacerConfig) }
    };

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var typeName = reader.GetString();
            return TypeMap.ContainsKey(typeName)
                ? (T)Activator.CreateInstance(TypeMap[typeName])
                : throw new JsonException($"Unknown operator type: {typeName}");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var obj = doc.RootElement;

        if (obj.ValueKind == JsonValueKind.Object && obj.EnumerateObject().MoveNext())
        {
            var property = obj.EnumerateObject().First();
            return TypeMap.ContainsKey(property.Name)
                ? (T)JsonSerializer.Deserialize(property.Value.GetRawText(), TypeMap[property.Name], options)
                : throw new JsonException($"Unknown operator type: {property.Name}");
        }

        throw new JsonException("Invalid format for operator configuration.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var typeName = TypeMap.FirstOrDefault(kvp => kvp.Value == value.GetType()).Key;
        if (typeName == null) throw new JsonException($"Unknown operator type: {value.GetType().Name}");

        if (value.GetType().GetProperties().Length == 0)
        {
            writer.WriteStringValue(typeName);
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(typeName);
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
            writer.WriteEndObject();
        }
    }
}

public static class ConfigSerializer
{
  private static readonly ISerializer YamlSerializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

  private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    Converters = { 
      new OperatorConverter<CreatorConfig>(), 
      new OperatorConverter<CrossoverConfig>(), 
      new OperatorConverter<MutatorConfig>(), 
      new OperatorConverter<SelectorConfig>(), 
      new OperatorConverter<ReplacerConfig>() 
    },
    WriteIndented = true
  };

  public static string SerializeToJson(GeneticAlgorithmConfig config) =>
    JsonSerializer.Serialize(config, JsonOptions);

  public static GeneticAlgorithmConfig DeserializeFromJson(string json) =>
    JsonSerializer.Deserialize<GeneticAlgorithmConfig>(json, JsonOptions)!;

  public static string SerializeToYaml(GeneticAlgorithmConfig config) =>
    YamlSerializer.Serialize(config);

  public static GeneticAlgorithmConfig DeserializeFromYaml(string yaml) =>
    YamlDeserializer.Deserialize<GeneticAlgorithmConfig>(yaml);
}
