namespace HEAL.HeuristicLib.Problems.Dynamic;

public class EvaluationClock : IEpochClock {
  private readonly Lock epochLocker = new();

  public int PendingEpochs { get; private set; }
  public int EpochCount { get; private set; }
  public required int EpochLength { get; init; }
  public int CurrentEpoch => EpochCount / EpochLength;

  //returns whether no unresolved epoch changes are pending
  public EvaluationTiming IncreaseCount() {
    lock (epochLocker) {
      EpochCount++;
      var valid = PendingEpochs == 0;
      if (EpochCount % EpochLength == 0)
        PendingEpochs++;
      return new EvaluationTiming(EpochCount, CurrentEpoch, valid);
    }
  }

  public void ResolvePendingEpochs(Action update) {
    lock (epochLocker) {
      var changed = PendingEpochs > 0;
      while (PendingEpochs > 0) {
        update();
        PendingEpochs--;
      }

      if (changed)
        OnEpochChange?.Invoke(this, CurrentEpoch);
    }
  }

  public event EventHandler<int>? OnEpochChange;

  public void AdvanceEpoch() {
    lock (epochLocker) {
      EpochCount = (CurrentEpoch + 1) * EpochLength;
      PendingEpochs++;
    }
  }
}
