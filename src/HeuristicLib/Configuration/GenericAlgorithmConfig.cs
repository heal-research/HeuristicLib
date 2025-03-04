using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HEAL.HeuristicLib.Configuration;

public record GeneticAlgorithmOptions(
  int? PopulationSize = null,
  int? MaximumGenerations = null,
  CreatorOptions? Creator = null,
  CrossoverOptions? Crossover = null,
  MutatorOptions? Mutator = null,
  double? MutationRate = null,
  SelectorOptions? Selector = null,
  ReplacerOptions? Replacer = null
);

public abstract record CreatorOptions();
public record RandomPermutationCreatorOptions : CreatorOptions;
public record UniformRealVectorCreatorOptions(double[]? Minimum, double[]? Maximum) : CreatorOptions;
public record NormalRealVectorCreatorOptions(double[]? Mean, double[]? StandardDeviation, double[]? Minimum, double[]? Maximum) : CreatorOptions;

public abstract record CrossoverOptions();
public record OrderCrossoverOptions : CrossoverOptions;
public record SinglePointRealVectorCrossoverOptions : CrossoverOptions;
public record AlphaBlendRealVectorCrossoverOptions(double? Alpha = 0.7, double? Beta = 0.3) : CrossoverOptions;

public abstract record MutatorOptions();
public record SwapMutatorOptions : MutatorOptions;
public record GaussianRealVectorMutatorOptions(double[]? Mean, double[]? StandardDeviation) : MutatorOptions;

public abstract record SelectorOptions();
public record TournamentSelectorOptions(int? TournamentSize = 3) : SelectorOptions;
public record RouletteWheelSelectorOptions : SelectorOptions;

public abstract record ReplacerOptions();
public record ElitistReplacerOptions : ReplacerOptions;

public class OptionsConfigSource<TGenotype, TEncoding> : IConfigSource<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype> {
  private readonly GeneticAlgorithmOptions options;

  public OptionsConfigSource(GeneticAlgorithmOptions options) {
    this.options = options;
  }


  public GeneticAlgorithmConfig<TGenotype, TEncoding> Apply(GeneticAlgorithmConfig<TGenotype, TEncoding> config) {
    var newConfig = config;

    if (options.PopulationSize.HasValue) {
      newConfig = newConfig with { PopulationSize = options.PopulationSize.Value };
    }
    
    if (options.MutationRate.HasValue) {
      newConfig = newConfig with { MutationRate = options.MutationRate.Value };
    }

    if (options.Creator is not null) {
      newConfig = newConfig with {
        CreatorFactory = (encoding, randomSource) => {
          IOperator @operator = (options.Creator, encoding) switch {
            (RandomPermutationCreatorOptions randomPermutationCreator, PermutationEncoding permutationEncoding) => new RandomPermutationCreator(permutationEncoding, randomSource),
            (UniformRealVectorCreatorOptions uniformRealVectorCreator, RealVectorEncoding realVectorEncoding) => new UniformDistributedCreator(realVectorEncoding, randomSource),
            (NormalRealVectorCreatorOptions normalRealVectorCreator, RealVectorEncoding realVectorEncoding) => new NormalDistributedCreator(realVectorEncoding, new RealVector(normalRealVectorCreator.Mean), new RealVector(normalRealVectorCreator.StandardDeviation), randomSource),
            _ => throw new NotImplementedException("Unknown creator configuration.")
          };
          if (@operator is not ICreator<TGenotype> creator) throw new InvalidOperationException("Creator must be a ICreator<TGenotype>.");
          return creator;
        }
      };
    }

    if (options.Crossover is not null) {
      newConfig = newConfig with {
        CrossoverFactory = (encoding, randomSource) => {
          IOperator @operator = (options.Crossover, encoding) switch {
            (OrderCrossoverOptions orderCrossover, PermutationEncoding permutationEncoding) => new OrderCrossover(permutationEncoding),
            (SinglePointRealVectorCrossoverOptions singlePointRealVectorCrossover, RealVectorEncoding realVectorEncoding) => new SinglePointCrossover(realVectorEncoding, randomSource),
            (AlphaBlendRealVectorCrossoverOptions alphaBlendRealVectorCrossoverOptions, RealVectorEncoding realVectorEncoding) => new AlphaBetaBlendCrossover(realVectorEncoding, alphaBlendRealVectorCrossoverOptions.Alpha.Value, alphaBlendRealVectorCrossoverOptions.Beta.Value),
            _ => throw new NotImplementedException("Unknown crossover configuration.")
          };
          if (@operator is not ICrossover<TGenotype> crossover) throw new InvalidOperationException("Crossover must be a ICrossover<TGenotype>.");
          return crossover;
        }
      };
    }

    if (options.MaximumGenerations.HasValue) {
      newConfig = newConfig with { Terminator = new ThresholdTerminator<PopulationState<TGenotype>>(options.MaximumGenerations.Value, state => state.CurrentGeneration) };
    }
    return newConfig;
  }
}
//
// public static class GeneticAlgorithmBuilderWithConfigurationExtension {
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithConfiguration<TEncoding, TGenotype>(
//     this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, GeneticAlgorithmOptions options) where TEncoding : IEncoding<TGenotype> 
//   {
//     if (options.PopulationSize.HasValue) builder.WithPopulationSize(options.PopulationSize.Value);
//     if (options.MaximumGenerations.HasValue) builder.WithTerminateOnMaxGenerations(options.MaximumGenerations.Value);
//     
//     if (options.Creator != null) builder.WithCreatorFactory(CreateCreatorFactory<TGenotype, TEncoding>(options.Creator));
//     if (options.Crossover != null) builder.WithCrossover(options.Crossover);
//     if (options.Mutator != null) builder.WithMutator(options.Mutator);
//     if (options.MutationRate.HasValue) builder.WithMutationRate(options.MutationRate.Value);
//     if (options.Selector != null) builder.WithSelector(options.Selector);
//     if (options.Replacer != null) builder.WithReplacer(options.Replacer);
//     return builder;
//   }
//
//   private static Func<TEncoding, RandomSource, ICreator<TGenotype>> CreateCreatorFactory<TGenotype, TEncoding>(CreatorOptions creatorOptions) where TEncoding : IEncoding<TGenotype> {
//     return (TEncoding encoding, RandomSource randomSource) => (creatorConfig: creatorOptions, encoding) switch {
//       (RandomPermutationCreatorOptions randomPermutationCreatorConfig, PermutationEncoding permutationEncoding) => new RandomPermutationCreator(permutationEncoding, randomSource),
//       (UniformRealVectorCreatorOptions uniformRealVectorCreatorConfig, RealVectorEncoding realVectorEncoding) => new UniformDistributedCreator(realVectorEncoding, randomSource),
//       (NormalRealVectorCreatorOptions normalRealVectorCreatorConfig, RealVectorEncoding realVectorEncoding) => new NormalDistributedCreator(realVectorEncoding, new RealVector(normalRealVectorCreatorConfig.Mean), new RealVector(normalRealVectorCreatorConfig.StandardDeviation), randomSource),
//       _ => throw new NotImplementedException("Unknown creator configuration.")
//     };
//   }
// }

//
// public class OperatorConverter<T> : JsonConverter<T> where T : class
// {
//     private static readonly Dictionary<string, Type> TypeMap = new()
//     {
//         { "RandomPermutationCreator", typeof(RandomPermutationCreatorConfig) },
//         { "UniformRealVectorCreator", typeof(UniformRealVectorCreatorConfig) },
//         { "NormalRealVectorCreator", typeof(NormalRealVectorCreatorConfig) },
//         { "OrderCrossover", typeof(OrderCrossoverConfig) },
//         { "SinglePointRealVectorCrossover", typeof(SinglePointRealVectorCrossoverConfig) },
//         { "AlphaBlendRealVectorCrossover", typeof(AlphaBlendRealVectorCrossoverConfig) },
//         { "SwapMutator", typeof(SwapMutatorConfig) },
//         { "GaussianRealVectorMutator", typeof(GaussianRealVectorMutatorConfig) },
//         { "TournamentSelector", typeof(TournamentSelectorConfig) },
//         { "RouletteWheelSelector", typeof(RouletteWheelSelectorConfig) },
//         { "ElitistReplacer", typeof(ElitistReplacerConfig) }
//     };
//
//     public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         if (reader.TokenType == JsonTokenType.String)
//         {
//             var typeName = reader.GetString();
//             return TypeMap.ContainsKey(typeName)
//                 ? (T)Activator.CreateInstance(TypeMap[typeName])
//                 : throw new JsonException($"Unknown operator type: {typeName}");
//         }
//
//         using var doc = JsonDocument.ParseValue(ref reader);
//         var obj = doc.RootElement;
//
//         if (obj.ValueKind == JsonValueKind.Object && obj.EnumerateObject().MoveNext())
//         {
//             var property = obj.EnumerateObject().First();
//             return TypeMap.ContainsKey(property.Name)
//                 ? (T)JsonSerializer.Deserialize(property.Value.GetRawText(), TypeMap[property.Name], options)
//                 : throw new JsonException($"Unknown operator type: {property.Name}");
//         }
//
//         throw new JsonException("Invalid format for operator configuration.");
//     }
//
//     public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
//     {
//         var typeName = TypeMap.FirstOrDefault(kvp => kvp.Value == value.GetType()).Key;
//         if (typeName == null) throw new JsonException($"Unknown operator type: {value.GetType().Name}");
//
//         if (value.GetType().GetProperties().Length == 0)
//         {
//             writer.WriteStringValue(typeName);
//         }
//         else
//         {
//             writer.WriteStartObject();
//             writer.WritePropertyName(typeName);
//             JsonSerializer.Serialize(writer, value, value.GetType(), options);
//             writer.WriteEndObject();
//         }
//     }
// }
//
// public static class ConfigSerializer
// {
//   private static readonly ISerializer YamlSerializer = new SerializerBuilder()
//     .WithNamingConvention(CamelCaseNamingConvention.Instance)
//     .Build();
//
//   private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
//     .WithNamingConvention(CamelCaseNamingConvention.Instance)
//     .Build();
//
//   private static readonly JsonSerializerOptions JsonOptions = new()
//   {
//     Converters = { 
//       new OperatorConverter<CreatorConfig>(), 
//       new OperatorConverter<CrossoverConfig>(), 
//       new OperatorConverter<MutatorConfig>(), 
//       new OperatorConverter<SelectorConfig>(), 
//       new OperatorConverter<ReplacerConfig>() 
//     },
//     WriteIndented = true
//   };
//
//   public static string SerializeToJson(GeneticAlgorithmConfig config) =>
//     JsonSerializer.Serialize(config, JsonOptions);
//
//   public static GeneticAlgorithmConfig DeserializeFromJson(string json) =>
//     JsonSerializer.Deserialize<GeneticAlgorithmConfig>(json, JsonOptions)!;
//
//   public static string SerializeToYaml(GeneticAlgorithmConfig config) =>
//     YamlSerializer.Serialize(config);
//
//   public static GeneticAlgorithmConfig DeserializeFromYaml(string yaml) =>
//     YamlDeserializer.Deserialize<GeneticAlgorithmConfig>(yaml);
// }
