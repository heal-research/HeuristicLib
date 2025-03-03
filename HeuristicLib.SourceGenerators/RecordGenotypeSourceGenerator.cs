using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace HEAL.HeuristicLib.SourceGenerators;

[Generator]
public class RecordGenotypeSourceGenerator : IIncrementalGenerator {
  public void Initialize(IncrementalGeneratorInitializationContext context) {
    var source = context.CompilationProvider.Select((compilation, cancellationToken) => {
      var sb = new StringBuilder();

      for (int n = 2; n <= 7; n++) {
        GenerateIRecordGenotypeBase(sb, n);
        GenerateRecordCrossover(sb, n);
        GenerateRecordMutator(sb, n);
        GenerateRecordCreator(sb, n);
      }

      return SourceText.From(sb.ToString(), Encoding.UTF8);
    });

    context.RegisterSourceOutput(source, action: (context, sourceText) => {
      context.AddSource("RecordGenotypeGenerated", sourceText);
    });
  }

  private void GenerateIRecordGenotypeBase(StringBuilder sb, int n) {
    string generics = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i}"));
    string parameters = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i} item{i}"));
    string deconstruct = string.Join("; ", Enumerable.Range(1, n).Select(i => $"item{i} = Item{i}"));

    sb.AppendLine($$"""
      public interface IRecordGenotypeBase<TSelf, {{generics}}> where TSelf : IRecordGenotypeBase<TSelf, {{generics}}> {
          static abstract TSelf Construct({{parameters}});
          void Deconstruct(out {{parameters}});
      }
      """);
  }

  private void GenerateRecordCrossover(StringBuilder sb, int n) {
    string generics = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i}"));
    string parameters = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i} item{i}"));
    string crossovers = string.Join(", ", Enumerable.Range(1, n).Select(i => $"private readonly ICrossover<T{i}> crossover{i};"));
    string constructorParams = string.Join(", ", Enumerable.Range(1, n).Select(i => $"ICrossover<T{i}> crossover{i}"));
    string assignCrossovers = string.Join("\n", Enumerable.Range(1, n).Select(i => $"this.crossover{i} = crossover{i};"));
    string crossoverLogic = string.Join("\n", Enumerable.Range(1, n).Select(i => $"var child{i} = crossover{i}.Crossover(parent1Chromosome{i}, parent2Chromosome{i});"));
    string constructParams = string.Join(", ", Enumerable.Range(1, n).Select(i => $"child{i}"));

    sb.AppendLine($$"""
      public class RecordCrossover<T, {{generics}}> : CrossoverBase<T> where T : IRecordGenotypeBase<T, {{generics}}> {
          {{crossovers}}
          
          public RecordCrossover({{constructorParams}}) {
              {{assignCrossovers}}
          }
          
          public override T Crossover(T parent1, T parent2) {
              var ({{parameters}}) = parent1;
              var ({{parameters}}) = parent2;
              {{crossoverLogic}}
              return T.Construct({{constructParams}});
          }
      }
      """);
  }

  private void GenerateRecordMutator(StringBuilder sb, int n) {
    string generics = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i}"));
    string parameters = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i} item{i}"));
    string mutators = string.Join(", ", Enumerable.Range(1, n).Select(i => $"private readonly IMutator<T{i}> mutator{i};"));
    string constructorParams = string.Join(", ", Enumerable.Range(1, n).Select(i => $"IMutator<T{i}> mutator{i}"));
    string assignMutators = string.Join("\n", Enumerable.Range(1, n).Select(i => $"this.mutator{i} = mutator{i};"));
    string mutateLogic = string.Join("\n", Enumerable.Range(1, n).Select(i => $"var mutated{i} = mutator{i}.Mutate(item{i});"));
    string constructParams = string.Join(", ", Enumerable.Range(1, n).Select(i => $"mutated{i}"));

    sb.AppendLine($$"""
      public class RecordMutator<T, {{generics}}> : MutatorBase<T> where T : IRecordGenotypeBase<T, {{generics}}> {
          {{mutators}}
          
          public RecordMutator({{constructorParams}}) {
              {{assignMutators}}
          }
          
          public override T Mutate(T individual) {
              var ({{parameters}}) = individual;
              {{mutateLogic}}
              return T.Construct({{constructParams}});
          }
      }
      """);
  }

  private void GenerateRecordCreator(StringBuilder sb, int n) {
    string generics = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i}"));
    string parameters = string.Join(", ", Enumerable.Range(1, n).Select(i => $"T{i} item{i}"));
    string creators = string.Join(", ", Enumerable.Range(1, n).Select(i => $"private readonly ICreator<T{i}> creator{i};"));
    string constructorParams = string.Join(", ", Enumerable.Range(1, n).Select(i => $"ICreator<T{i}> creator{i}"));
    string assignCreators = string.Join("\n", Enumerable.Range(1, n).Select(i => $"this.creator{i} = creator{i};"));
    string createLogic = string.Join("\n", Enumerable.Range(1, n).Select(i => $"var newItem{i} = creator{i}.Create();"));
    string constructParams = string.Join(", ", Enumerable.Range(1, n).Select(i => $"newItem{i}"));

    sb.AppendLine($$"""
      public class RecordCreator<T, {{generics}}> : CreatorBase<T> where T : IRecordGenotypeBase<T, {{generics}}> {
          {{creators}}
          
          public RecordCreator({{constructorParams}}) {
              {{assignCreators}}
          }
          
          public override T Create() {
              {{createLogic}}
              return T.Construct({{constructParams}});
          }
      }
      """);
  }
}
