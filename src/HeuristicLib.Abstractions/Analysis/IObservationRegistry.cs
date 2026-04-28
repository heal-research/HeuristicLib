using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

public interface IObservationRegistry
{
  void Add(IOperatorObservationInstaller installer);
}

public interface IOperatorObservationInstaller
{
  IOperator Operator { get; }

  bool TryMerge(IOperatorObservationInstaller other);

  void Install(ExecutionInstanceRegistry registry);
}

internal sealed class ObservationRegistry : IObservationRegistry
{
  private readonly Dictionary<IOperator, IOperatorObservationInstaller> installers = new(ReferenceEqualityComparer.Instance);

  public IReadOnlyCollection<IOperatorObservationInstaller> Installers => installers.Values;

  public void Add(IOperatorObservationInstaller installer)
  {
    if (installers.TryGetValue(installer.Operator, out var existingInstaller)) {
      if (existingInstaller.TryMerge(installer)) {
        return;
      }

      throw new InvalidOperationException($"Observation installer conflict for operator {installer.Operator}.");
    }

    installers.Add(installer.Operator, installer);
  }
}
