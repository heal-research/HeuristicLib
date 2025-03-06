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

public record GeneticAlgorithmSpec(
  int? PopulationSize = null,
  int? MaximumGenerations = null,
  CreatorSpec? Creator = null,
  CrossoverSpec? Crossover = null,
  MutatorSpec? Mutator = null,
  double? MutationRate = null,
  SelectorSpec? Selector = null,
  ReplacerSpec? Replacer = null,
  int? RandomSeed = null
);

public abstract record OperatorSpec();

public abstract record CreatorSpec() : OperatorSpec();
public record RandomPermutationCreatorSpec : CreatorSpec;
public record UniformRealVectorCreatorSpec(double[]? Minimum = null, double[]? Maximum = null) : CreatorSpec;
public record NormalRealVectorCreatorSpec(double[]? Mean = null, double[]? StandardDeviation = null) : CreatorSpec;

public abstract record CrossoverSpec() : OperatorSpec();
public record OrderCrossoverSpec : CrossoverSpec;
public record SinglePointRealVectorCrossoverSpec : CrossoverSpec;
public record AlphaBlendRealVectorCrossoverSpec(double? Alpha = null, double? Beta = null) : CrossoverSpec;

public abstract record MutatorSpec(): OperatorSpec();
public record SwapMutatorSpec : MutatorSpec;
public record GaussianRealVectorMutatorSpec(double? Rate = null, double? Strength = null) : MutatorSpec;

public abstract record SelectorSpec(): OperatorSpec();
public record TournamentSelectorSpec(int? TournamentSize = null) : SelectorSpec;
public record RouletteWheelSelectorSpec : SelectorSpec;

public abstract record ReplacerSpec(): OperatorSpec();
public record ElitistReplacerSpec : ReplacerSpec;



public class SpecConfigSource<TGenotype, TEncoding> : IConfigSource<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
  private readonly GeneticAlgorithmSpec gaSpec;

  public SpecConfigSource(GeneticAlgorithmSpec gaSpec) {
    this.gaSpec = gaSpec;
  }

  public GeneticAlgorithmConfig<TGenotype, TEncoding> Apply(GeneticAlgorithmConfig<TGenotype, TEncoding> config) {
    var newConfig = config;

    if (gaSpec.PopulationSize.HasValue) {
      newConfig = newConfig with { PopulationSize = gaSpec.PopulationSize.Value };
    }
    
    if (gaSpec.MutationRate.HasValue) {
      newConfig = newConfig with { MutationRate = gaSpec.MutationRate.Value };
    }
#pragma warning disable S1481
    if (gaSpec.Creator is not null) {
      newConfig = newConfig with {
        CreatorFactory = (encoding, randomSource) => {
          IOperator @operator = (gaSpec.Creator, encoding) switch {
            (RandomPermutationCreatorSpec spec, PermutationEncoding enc) => new RandomPermutationCreator(enc, randomSource),
            (UniformRealVectorCreatorSpec spec, RealVectorEncoding enc) => new UniformDistributedCreator(enc, spec.Minimum != null ? new RealVector(spec.Minimum) : null, spec.Maximum != null ? new RealVector(spec.Maximum) : null, randomSource),
            (NormalRealVectorCreatorSpec spec, RealVectorEncoding enc) => new NormalDistributedCreator(enc, spec.Mean != null ? new RealVector(spec.Mean) : 0.0, spec.StandardDeviation != null ? new RealVector(spec.StandardDeviation) : 1.0, randomSource),
            _ => throw new NotImplementedException("Unknown creator configuration.")
          };
          if (@operator is not ICreator<TGenotype> creator) throw new InvalidOperationException("Creator must be a ICreator<TGenotype>.");
          return creator;
        }
      };
    }

    if (gaSpec.Crossover is not null) {
      newConfig = newConfig with {
        CrossoverFactory = (encoding, randomSource) => {
          IOperator @operator = (gaSpec.Crossover, encoding) switch {
            (OrderCrossoverSpec spec, PermutationEncoding enc) => new OrderCrossover(enc),
            (SinglePointRealVectorCrossoverSpec spec, RealVectorEncoding enc) => new SinglePointCrossover(enc, randomSource),
            (AlphaBlendRealVectorCrossoverSpec spec, RealVectorEncoding enc) => new AlphaBetaBlendCrossover(enc, spec.Alpha, spec.Beta),
            _ => throw new NotImplementedException("Unknown crossover configuration.")
          };
          if (@operator is not ICrossover<TGenotype> crossover) throw new InvalidOperationException("Crossover must be a ICrossover<TGenotype>.");
          return crossover;
        }
      };
    }

    if (gaSpec.Mutator is not null) {
      newConfig = newConfig with {
        MutatorFactory = (encoding, randomSource) => {
          IOperator @operator = (gaSpec.Mutator, encoding) switch {
            (SwapMutatorSpec spec, PermutationEncoding enc) => new SwapMutator(enc),
            (GaussianRealVectorMutatorSpec spec, RealVectorEncoding enc) => new GaussianMutator(enc, spec.Strength ?? 1.0, spec.Rate ?? 1.0, randomSource),
            _ => throw new NotImplementedException("Unknown mutator configuration.")
          };
          if (@operator is not IMutator<TGenotype> mutator) throw new InvalidOperationException("Mutator must be a IMutator<TGenotype>.");
          return mutator;
        }
      };
    }

    if (gaSpec.MaximumGenerations.HasValue) {
      newConfig = newConfig with { Terminator = new ThresholdTerminator<PopulationState<TGenotype>>(gaSpec.MaximumGenerations.Value, state => state.Generation) };
    }

    if (gaSpec.RandomSeed.HasValue) {
      newConfig = newConfig with { RandomSource = new SeededRandomSource(gaSpec.RandomSeed.Value) };
    }
#pragma warning restore S1481
    return newConfig;
  }
}

public static class SpecsConfigSourceBuilderExtension {
  public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithSpecs<TEncoding, TGenotype>
    (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, GeneticAlgorithmSpec gaSpec) 
    where TEncoding : IEncoding<TGenotype, TEncoding> {
    return builder.AddSource(new SpecConfigSource<TGenotype, TEncoding>(gaSpec));
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
