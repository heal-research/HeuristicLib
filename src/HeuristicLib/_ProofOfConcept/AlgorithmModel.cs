using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib._ProofOfConcept;

public interface IAlgorithmModel
{
  IReadOnlyList<Parameter> Parameters { get; }
  IAlgorithm CreateAlgorithm();
}

public abstract class AlgorithmModelBase
  : IAlgorithmModel {
  protected readonly List<Parameter> parameters = [];
  public IReadOnlyList<Parameter> Parameters => parameters;

  public abstract IAlgorithm CreateAlgorithm();
}

public abstract record Parameter(string Name, string Description);

public record Parameter<T>(string Name, string Description, T DefaultValue) : Parameter(Name, Description) {
  public T Value { get; set; }
};

public partial class GeneticAlgorithmModel
  : AlgorithmModelBase
{
  public GeneticAlgorithmModel()
  {
    PopulationSize = new Parameter<int>("Population Size", "Number of individuals per generation.", 100);
    MaxGenerations = new Parameter<int>("Max Generations", "Maximum number of generations to run.", 50);
    AutoAddParameters();
  }

  public Parameter<int> PopulationSize { get; }
  public Parameter<int> MaxGenerations { get; }

  private partial void AutoAddParameters();
  public override GeneticAlgorithm<double[]> CreateAlgorithm() {
    return null!;
    // return new GeneticAlgorithm(
    //   PopulationSize.Value,
    //   new ThresholdCriterion<PopulationState>(MaxGenerations.Value, state => state.CurrentGeneration)
    // );
  }
}

// GENERATED
public partial class GeneticAlgorithmModel {
  private partial void AutoAddParameters()
  {
    parameters.Add(PopulationSize);
    parameters.Add(MaxGenerations);
  }
}

class AlgorithmModelExample
{
  // void Execute() {
  //   var pauseToken = new PauseToken();
  //
  //   var ga = new GeneticAlgorithmBuilder<double[]>()
  //     .WithPopulationSize(200)
  //     .TerminateOnMaxGenerations(50)
  //     .TerminateOnPauseToken(pauseToken)
  //     .OnLiveResult(best => Console.WriteLine($"Best so far: {string.Join(",", best)}"))
  //     .Build();
  //
  //   Task.Run(async () =>
  //   {
  //     await Task.Delay(1000);
  //     pauseToken.RequestPause();
  //   });
  //
  //   var finalState = ga.Run();
  //   Console.WriteLine($"Paused at generation {finalState.CurrentGeneration}");
  // }
}
